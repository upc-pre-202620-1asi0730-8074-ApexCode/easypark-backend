<#
EasyPark: prueba funcional de las 60 operaciones REST reales.
Requisitos: PowerShell 5.1+, .NET 10, Docker Desktop, carpeta backend con .env.
Uso: Colocar este archivo en backend\scripts\Test-Todos-Endpoints-EasyPark.ps1 y ejecutar:
  .\scripts\Test-Todos-Endpoints-EasyPark.ps1

Seguridad: SOLO localhost/127.0.0.1. Crea cuentas, vehiculos, reservas y otros
registros temporales (no elimina registros, porque la API no permite borrarlos).
Produce CSV y JSON en backend\test-results. Si la API no esta encendida,
enciende MySQL y luego inicia la API con dotnet run.
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:8080',
    [switch]$OnlyTest,
    [switch]$RunUnitTests
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$BaseUrl = $BaseUrl.TrimEnd('/')
if ($BaseUrl -notmatch '^http://(localhost|127\.0\.0\.1):\d+$') {
    throw 'Por seguridad solo se admite http://localhost:PUERTO o http://127.0.0.1:PUERTO.'
}
$Root = Split-Path -Parent $PSScriptRoot
$EnvFile = Join-Path $Root '.env'
$EndpointDoc = Join-Path $Root 'docs\API-ENDPOINTS.md'
$HasSolution = (Test-Path (Join-Path $Root 'EasyPark.Platform.sln')) -or (Test-Path (Join-Path $Root 'EasyPark.Platform.slnx'))
if (!$HasSolution -or !(Test-Path $EndpointDoc)) {
    throw 'Coloca este script dentro de la carpeta backend\scripts del ZIP de EasyPark.'
}
$ReportDir = Join-Path $Root 'test-results'
New-Item -Path $ReportDir -ItemType Directory -Force | Out-Null
$RunTag = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)
$script:Results = New-Object System.Collections.Generic.List[object]
$script:Executed = @{}
$script:ApiOwned = $false
$script:ApiProcess = $null
$script:LastFailure = ''
$pass = 'Test12345!'
$stamp = [Guid]::NewGuid().ToString('N').Substring(0,12)
$opEmail = "e2e-operator-$stamp@easypark.local"
$driverEmail = "e2e-driver-$stamp@easypark.local"

function Read-Env([string]$Key) {
    if (!(Test-Path $EnvFile)) { throw "Falta .env: $EnvFile" }
    $record = Get-Content $EnvFile | Where-Object { $_.StartsWith($Key + '=') } | Select-Object -First 1
    if (!$record) { throw "Falta $Key en .env" }
    return $record.Substring($Key.Length + 1)
}
function Is-Ready {
    try {
        $r = Invoke-RestMethod -Method Get -Uri "$BaseUrl/health/ready" -TimeoutSec 3 -ErrorAction Stop
        return $r.status -eq 'ready'
    } catch { return $false }
}
function Initialize-Api {
    if (Is-Ready) {
        Write-Host "[SERVICIO] API ya activa: $BaseUrl" -ForegroundColor Green
        return
    }
    if ($OnlyTest) { throw 'La API no responde y se solicito -OnlyTest.' }
    Write-Host '[SERVICIO] Iniciando MySQL con Docker Compose...' -ForegroundColor Cyan
    Push-Location $Root
    try { & docker compose up -d mysql | Out-Host; if ($LASTEXITCODE -ne 0) { throw 'Fallo Docker Compose.' } }
    finally { Pop-Location }
    $mysqlPw = Read-Env 'MYSQL_PASSWORD'
    $jwtPw = Read-Env 'JWT_SECRET'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__DefaultConnection = "Server=127.0.0.1;Port=3307;Database=easypark;User=easypark;Password=$mysqlPw;SslMode=Disabled;AllowPublicKeyRetrieval=True;"
    $env:TokenSettings__Secret = $jwtPw
    $env:SeedDemoData = 'true'
    $env:Database__Migrate = 'true'
    $env:ASPNETCORE_URLS = $BaseUrl
    $standardOut = Join-Path $ReportDir "api-$RunTag.stdout.log"
    $standardErr = Join-Path $ReportDir "api-$RunTag.stderr.log"
    Write-Host '[SERVICIO] Iniciando ASP.NET Core...' -ForegroundColor Cyan
    $script:ApiProcess = Start-Process -FilePath 'dotnet' -ArgumentList @('run','--no-launch-profile','--project','EasyPark.Platform\EasyPark.Platform.csproj') -WorkingDirectory $Root -PassThru -RedirectStandardOutput $standardOut -RedirectStandardError $standardErr
    $script:ApiOwned = $true
    $end = (Get-Date).AddSeconds(120)
    while ((Get-Date) -lt $end) {
        if (Is-Ready) { Write-Host "[SERVICIO] API lista: $BaseUrl" -ForegroundColor Green; return }
        if ($script:ApiProcess.HasExited) {
            $tail = if (Test-Path $standardErr) { (Get-Content $standardErr -Tail 30) -join "`n" } else { '' }
            throw "La API termino al arrancar. Log: $standardErr`n$tail"
        }
        Start-Sleep -Seconds 2
    }
    throw "La API no estuvo lista. Revisa $standardOut y $standardErr"
}

# Estandariza el tratamiento de fallos REST (PowerShell 5.1 y 7).
function Api-Request([string]$Method, [string]$Path, $Body, [string]$Token) {
    $p = @{
        Uri="$BaseUrl$Path"; Method=$Method; TimeoutSec=25; ErrorAction='Stop'; UseBasicParsing=$true
    }
    if ($Token) { $p.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) {
        $p.ContentType = 'application/json; charset=utf-8'
        $json = ConvertTo-Json -InputObject $Body -Depth 16 -Compress
        $p.Body = [System.Text.Encoding]::UTF8.GetBytes($json)
    }
    try {
        $res = Invoke-WebRequest @p
        $content = [string]$res.Content
        $status = [int]$res.StatusCode
    } catch {
        $status = 0
        $content = ''
        if ($_.Exception.Response) {
            $resp = $_.Exception.Response
            if ($resp -is [System.Net.HttpWebResponse]) {
                $status = [int]$resp.StatusCode
                try {
                    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
                    $content = $reader.ReadToEnd()
                    $reader.Dispose()
                } catch { }
            } elseif ($resp.StatusCode) {
                $status = [int]$resp.StatusCode
                try { $content = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult() } catch { }
            }
        }
        if (!$status) { throw }
    }
    $obj = $null
    if ($content -and $content.Trim().StartsWith('{')) {
        try { $obj = ConvertFrom-Json -InputObject $content } catch { $obj = $content }
    } elseif ($content -and $content.Trim().StartsWith('[')) {
        try { $obj = ConvertFrom-Json -InputObject $content } catch { $obj = $content }
    } else { $obj = $content }
    return [pscustomobject]@{ Status=$status; Body=$obj; Raw=$content }
}

function Check-Api {
    param(
        [Parameter(Mandatory=$true)][string]$Method,
        [Parameter(Mandatory=$true)][string]$Template,
        [string]$Path = $Template,
        $Body = $null,
        [string]$Token = '',
        [int[]]$Expect = @(200),
        [scriptblock]$Assert = $null
    )
    $key = "$Method $Template"
    $script:Executed[$key] = $true
    $t0 = Get-Date
    $status = 0; $message = ''; $value = $null; $ok = $false
    try {
        $result = Api-Request -Method $Method -Path $Path -Body $Body -Token $Token
        $status = [int]$result.Status
        $value = $result.Body
        if ($Expect -notcontains $status) {
            $brief = $result.Raw
            if ($brief.Length -gt 280) { $brief = $brief.Substring(0,280) }
            throw "Se esperaba HTTP $($Expect -join '/') y se recibio HTTP $status. $brief"
        }
        if ($Assert) { & $Assert $value }
        $ok = $true
    } catch {
        $message = $_.Exception.Message
        if ($message.Length -gt 350) { $message = $message.Substring(0,350) }
        $script:LastFailure = $message
    }
    $ms = [int]((Get-Date)-$t0).TotalMilliseconds
    $script:Results.Add([pscustomobject]@{ Metodo=$Method; Ruta=$Template; HTTP=$status; Resultado=$(if ($ok) { 'OK' } else { 'ERROR' }); Milisegundos=$ms; Detalle=$message })
    $color = if ($ok) { 'Green' } else { 'Red' }
    Write-Host ("[CALL {0,2}] {1,-5} {2,-48} HTTP {3,-3} {4}" -f $script:Results.Count, $Method, $Template, $status, $(if ($ok) { 'OK' } else { "ERROR: $message" })) -ForegroundColor $color
    return $value
}

function Must-Id($Value, [string]$Context) {
    if ($null -eq $Value -or $null -eq $Value.id -or [int]$Value.id -le 0) {
        throw "No se obtuvo un ID valido para $Context. Ultimo error: $script:LastFailure"
    }
    return [int]$Value.id
}
function Assert-Prop([object]$Data, [string]$Prop, [object]$Expected) {
    if ($null -eq $Data -or [string]$Data.$Prop -ne [string]$Expected) {
        throw "Propiedad ${Prop}: se esperaba '$Expected', se recibio '$($Data.$Prop)'."
    }
}

try {
    Initialize-Api
    if ($RunUnitTests) {
        Write-Host 'Ejecutando tambien las pruebas xUnit...' -ForegroundColor Cyan
        Push-Location $Root
        try { & dotnet test --configuration Release; if ($LASTEXITCODE -ne 0) { throw 'Fallo dotnet test.' } }
        finally { Pop-Location }
    }

    # A. IAM y perfiles. Todas las cuentas y registros de esta ejecucion son nuevos.
    Write-Host "`n=== A. Usuarios y perfiles ===" -ForegroundColor Cyan
    $op = Check-Api POST '/api/v1/authentication/sign-up' -Body @{email=$opEmail;password=$pass;role='OPERATOR_ADMIN'} -Expect @(201)
    $opId = Must-Id $op 'operador'
    $opLogin = Check-Api POST '/api/v1/authentication/sign-in' -Body @{email=$opEmail;password=$pass} -Expect @(200)
    if (!$opLogin.token) { throw 'No se obtuvo JWT del operador.' }
    $opToken = [string]$opLogin.token
    $operatorProfile = Check-Api POST '/api/v1/profiles' -Token $opToken -Body @{firstName='Auto';lastName='Operador';phone='+51900123456';companyName='EasyPark E2E';jobTitle='Administrador'} -Expect @(201)
    $opProfileId = Must-Id $operatorProfile 'perfil operador'
    $null = Check-Api GET '/api/v1/profiles' -Token $opToken
    $null = Check-Api GET '/api/v1/profiles/{id:int}' -Path "/api/v1/profiles/$opProfileId" -Token $opToken
    $null = Check-Api PUT '/api/v1/profiles/{id:int}' -Path "/api/v1/profiles/$opProfileId" -Token $opToken -Body @{firstName='Auto';lastName='Operador Editado';phone='+51900123457';companyName='EasyPark E2E';jobTitle='Supervisor'}

    # B. Parking Management: instalaciones propias y espacios nuevos.
    Write-Host "`n=== B. Parking Management ===" -ForegroundColor Cyan
    $facility = Check-Api POST '/api/v1/parking-facilities' -Token $opToken -Body @{operatorProfileId=$opProfileId;name="Sede E2E $stamp";address='Av. Pruebas 100, Lima';latitude=-12.12;longitude=-77.03;hourlyRate=6.5;openTime='06:00';closeTime='23:00';status='ACTIVE'} -Expect @(201)
    $facilityId = Must-Id $facility 'estacionamiento'
    $null = Check-Api GET '/api/v1/parking-facilities' -Token $opToken -Path '/api/v1/parking-facilities?onlyAvailable=true'
    $null = Check-Api GET '/api/v1/parking-facilities/{id:int}' -Path "/api/v1/parking-facilities/$facilityId" -Token $opToken
    $null = Check-Api PUT '/api/v1/parking-facilities/{id:int}' -Path "/api/v1/parking-facilities/$facilityId" -Token $opToken -Body @{name="Sede E2E Actualizada $stamp";address='Av. Pruebas 101, Lima';latitude=-12.12;longitude=-77.03;hourlyRate=7.5;openTime='06:00';closeTime='23:00';status='ACTIVE'}
    $spot1 = Check-Api POST '/api/v1/parking-spots' -Token $opToken -Body @{facilityId=$facilityId;code='A-01';level=1;spotType='CAR'} -Expect @(201)
    $spot1Id = Must-Id $spot1 'espacio 1'
    $spot2 = Check-Api POST '/api/v1/parking-spots' -Token $opToken -Body @{facilityId=$facilityId;code='A-02';level=1;spotType='CAR'} -Expect @(201)
    $spot2Id = Must-Id $spot2 'espacio 2'
    $spot3 = Check-Api POST '/api/v1/parking-spots' -Token $opToken -Body @{facilityId=$facilityId;code='A-03';level=1;spotType='CAR'} -Expect @(201)
    $spot3Id = Must-Id $spot3 'espacio 3'
    $null = Check-Api GET '/api/v1/parking-spots' -Path "/api/v1/parking-spots?facilityId=$facilityId" -Token $opToken
    $null = Check-Api GET '/api/v1/parking-spots/{id:int}' -Path "/api/v1/parking-spots/$spot1Id" -Token $opToken
    $null = Check-Api PUT '/api/v1/parking-spots/{id:int}' -Path "/api/v1/parking-spots/$spot1Id" -Token $opToken -Body @{code='A-01';level=2;spotType='CAR';status='AVAILABLE'}

    # C. Conductor y vehiculo propio.
    Write-Host "`n=== C. Conductor y vehiculo ===" -ForegroundColor Cyan
    $driver = Check-Api POST '/api/v1/authentication/sign-up' -Body @{email=$driverEmail;password=$pass;role='DRIVER'} -Expect @(201)
    $driverId = Must-Id $driver 'conductor'
    $driverLogin = Check-Api POST '/api/v1/authentication/sign-in' -Body @{email=$driverEmail;password=$pass} -Expect @(200)
    if (!$driverLogin.token) { throw 'No se obtuvo JWT del conductor.' }
    $driverToken = [string]$driverLogin.token
    $driverProfile = Check-Api POST '/api/v1/profiles' -Token $driverToken -Body @{firstName='Auto';lastName='Conductor';phone='+51900999888'} -Expect @(201)
    $driverProfileId = Must-Id $driverProfile 'perfil conductor'
    $plate = 'T' + (Get-Random -Minimum 10 -Maximum 99).ToString() + '-' + (Get-Random -Minimum 100 -Maximum 999).ToString()
    $v = Check-Api POST '/api/v1/vehicles' -Token $driverToken -Body @{profileId=$driverProfileId;plateNumber=$plate;vehicleType='CAR';color='Rojo';isDefault=$true} -Expect @(201)
    $vehicleId = Must-Id $v 'vehiculo'
    $null = Check-Api GET '/api/v1/vehicles' -Path "/api/v1/vehicles?profileId=$driverProfileId" -Token $driverToken
    $null = Check-Api GET '/api/v1/vehicles/{id:int}' -Path "/api/v1/vehicles/$vehicleId" -Token $driverToken
    $null = Check-Api PUT '/api/v1/vehicles/{id:int}' -Path "/api/v1/vehicles/$vehicleId" -Token $driverToken -Body @{profileId=$driverProfileId;plateNumber=$plate;vehicleType='CAR';color='Negro';isDefault=$true}

    # D. Reserva + entrada + reasignacion + salida; y movimientos en revision.
    Write-Host "`n=== D. Reservas y control de acceso ===" -ForegroundColor Cyan
    $reservation = Check-Api POST '/api/v1/reservations' -Token $driverToken -Body @{driverProfileId=$driverProfileId;vehicleId=$vehicleId;parkingSpotId=$spot1Id;startAt=[DateTime]::UtcNow.AddMinutes(30).ToString('o');durationMinutes=90} -Expect @(201) -Assert { param($x) Assert-Prop $x 'status' 'CONFIRMED' }
    $reservationId = Must-Id $reservation 'reserva 1'
    $null = Check-Api GET '/api/v1/reservations' -Token $driverToken
    $null = Check-Api GET '/api/v1/reservations/{id:int}' -Path "/api/v1/reservations/$reservationId" -Token $driverToken
    $status1 = Check-Api GET '/api/v1/parking-spots/{id:int}' -Path "/api/v1/parking-spots/$spot1Id" -Token $opToken -Assert { param($x) Assert-Prop $x 'status' 'RESERVED' }

    $unknownPlate = 'U' + (Get-Random -Minimum 10 -Maximum 99).ToString() + '-' + (Get-Random -Minimum 100 -Maximum 999).ToString()
    $review = Check-Api POST '/api/v1/access-movements' -Token $opToken -Body @{parkingFacilityId=$facilityId;plateNumber=$unknownPlate;movementType='ENTRY';registrationMethod='MANUAL'} -Expect @(201) -Assert {param($x) Assert-Prop $x 'status' 'UNDER_REVIEW'}
    $reviewId = Must-Id $review 'movimiento en revision'
    $null = Check-Api GET '/api/v1/access-movements' -Path "/api/v1/access-movements?parkingFacilityId=$facilityId" -Token $opToken
    $null = Check-Api GET '/api/v1/access-movements/{id:int}' -Path "/api/v1/access-movements/$reviewId" -Token $opToken
    $null = Check-Api PUT '/api/v1/access-movements/{id:int}' -Path "/api/v1/access-movements/$reviewId" -Token $opToken -Body @{status='REJECTED';note='Cerrado por test automatico'} -Assert {param($x) Assert-Prop $x 'status' 'REJECTED'}

    $entry = Check-Api POST '/api/v1/access-movements' -Token $opToken -Body @{parkingFacilityId=$facilityId;plateNumber=$plate;movementType='ENTRY';registrationMethod='MANUAL'} -Expect @(201) -Assert {param($x) Assert-Prop $x 'status' 'COMPLETED'}
    if (!$entry.parkingStayId) { throw 'El ingreso no creo parkingStayId.' }
    $stayId = [int]$entry.parkingStayId
    $null = Check-Api GET '/api/v1/parking-stays' -Path "/api/v1/parking-stays?parkingFacilityId=$facilityId&open=true" -Token $opToken
    $null = Check-Api GET '/api/v1/parking-stays/{id:int}' -Path "/api/v1/parking-stays/$stayId" -Token $opToken
    $null = Check-Api PUT '/api/v1/parking-stays/{id:int}' -Path "/api/v1/parking-stays/$stayId" -Token $opToken -Body @{parkingSpotId=$spot2Id} -Assert {param($x) Assert-Prop $x 'parkingSpotId' $spot2Id}
    $exit = Check-Api POST '/api/v1/access-movements' -Token $opToken -Body @{parkingFacilityId=$facilityId;plateNumber=$plate;movementType='EXIT';registrationMethod='MANUAL'} -Expect @(201) -Assert {param($x) Assert-Prop $x 'status' 'COMPLETED'}
    $closedStay = Check-Api GET '/api/v1/parking-stays/{id:int}' -Path "/api/v1/parking-stays/$stayId" -Token $opToken
    if (!$closedStay.exitMovementId) { throw 'La salida no cerro la permanencia.' }
    $released = Check-Api GET '/api/v1/parking-spots/{id:int}' -Path "/api/v1/parking-spots/$spot2Id" -Token $opToken -Assert {param($x) Assert-Prop $x 'status' 'AVAILABLE'}

    # Las dos rutas para cancelar se ejercitan en reservas separadas.
    $res2 = Check-Api POST '/api/v1/reservations' -Token $driverToken -Body @{vehicleId=$vehicleId;parkingSpotId=$spot1Id;startAt=[DateTime]::UtcNow.AddHours(2).ToString('o');durationMinutes=60} -Expect @(201)
    $res2Id = Must-Id $res2 'reserva para PUT cancellation'
    $null = Check-Api PUT '/api/v1/reservations/{id:int}' -Path "/api/v1/reservations/$res2Id" -Token $driverToken -Body @{status='CANCELLED'} -Assert {param($x) Assert-Prop $x 'status' 'CANCELLED'}
    $res3 = Check-Api POST '/api/v1/reservations' -Token $driverToken -Body @{vehicleId=$vehicleId;parkingSpotId=$spot3Id;startAt=[DateTime]::UtcNow.AddHours(3).ToString('o');durationMinutes=60} -Expect @(201)
    $res3Id = Must-Id $res3 'reserva para POST cancellation'
    $null = Check-Api POST '/api/v1/reservations/{id:int}/cancellation' -Path "/api/v1/reservations/$res3Id/cancellation" -Token $driverToken -Expect @(200) -Assert {param($x) Assert-Prop $x 'status' 'CANCELLED'}

    # E. Las siete tablas nuevas.
    Write-Host "`n=== E. Alertas, notificaciones, metricas y reportes ===" -ForegroundColor Cyan
    $ruleType = "TEST_$stamp"
    $rule = Check-Api POST '/api/v1/alert-rules' -Token $opToken -Body @{parkingFacilityId=$facilityId;alertType=$ruleType;severity='HIGH';threshold=120;enabled=$true} -Expect @(201)
    $ruleId = Must-Id $rule 'regla'
    $null = Check-Api GET '/api/v1/alert-rules' -Token $opToken
    $null = Check-Api GET '/api/v1/alert-rules/{id:int}' -Path "/api/v1/alert-rules/$ruleId" -Token $opToken
    $null = Check-Api PUT '/api/v1/alert-rules/{id:int}' -Path "/api/v1/alert-rules/$ruleId" -Token $opToken -Body @{parkingFacilityId=$facilityId;alertType=$ruleType;severity='MEDIUM';threshold=180;enabled=$true}

    $alert = Check-Api POST '/api/v1/alerts' -Token $opToken -Body @{parkingFacilityId=$facilityId;alertType=$ruleType;severity='HIGH';contextValue=200;parkingStayId=$stayId;accessMovementId=$reviewId} -Expect @(201)
    $alertId = Must-Id $alert 'alerta'
    $null = Check-Api GET '/api/v1/alerts' -Token $opToken
    $null = Check-Api GET '/api/v1/alerts/{id:int}' -Path "/api/v1/alerts/$alertId" -Token $opToken
    $null = Check-Api PUT '/api/v1/alerts/{id:int}' -Path "/api/v1/alerts/$alertId" -Token $opToken -Body @{status='RESOLVED';note='Validacion automatica'} -Assert {param($x) Assert-Prop $x 'status' 'RESOLVED'}

    $template = Check-Api POST '/api/v1/notification-templates' -Token $opToken -Body @{type="TEST_$stamp";locale='es';titleTemplate='Aviso automatico';bodyTemplate='Mensaje de prueba EasyPark'} -Expect @(201)
    $templateId = Must-Id $template 'plantilla'
    $null = Check-Api GET '/api/v1/notification-templates' -Token $driverToken
    $null = Check-Api GET '/api/v1/notification-templates/{id:int}' -Path "/api/v1/notification-templates/$templateId" -Token $driverToken
    $null = Check-Api PUT '/api/v1/notification-templates/{id:int}' -Path "/api/v1/notification-templates/$templateId" -Token $opToken -Body @{type="TEST_$stamp";locale='es';titleTemplate='Aviso actualizado';bodyTemplate='Mensaje actualizado EasyPark'}

    $notification = Check-Api POST '/api/v1/notifications' -Token $opToken -Body @{recipientId=$driverId;notificationTemplateId=$templateId;reservationId=$reservationId;parkingStayId=$stayId;alertId=$alertId;title='Prueba E2E';message='Notificacion automatica';channel='IN_APP'} -Expect @(201)
    $notificationId = Must-Id $notification 'notificacion'
    $null = Check-Api GET '/api/v1/notifications' -Token $driverToken
    $null = Check-Api GET '/api/v1/notifications/{id:int}' -Path "/api/v1/notifications/$notificationId" -Token $driverToken
    $null = Check-Api PUT '/api/v1/notifications/{id:int}' -Path "/api/v1/notifications/$notificationId" -Token $driverToken -Body @{status='READ'} -Assert {param($x) Assert-Prop $x 'status' 'READ'}

    $metric = Check-Api POST '/api/v1/occupancy-metrics' -Token $opToken -Body @{parkingFacilityId=$facilityId} -Expect @(201)
    $metricId = Must-Id $metric 'metrica'
    $null = Check-Api GET '/api/v1/occupancy-metrics' -Token $opToken
    $null = Check-Api GET '/api/v1/occupancy-metrics/{id:int}' -Path "/api/v1/occupancy-metrics/$metricId" -Token $opToken
    $null = Check-Api PUT '/api/v1/occupancy-metrics/{id:int}' -Path "/api/v1/occupancy-metrics/$metricId" -Token $opToken -Body @{parkingFacilityId=$facilityId}

    $from = [DateTime]::UtcNow.AddDays(-2).ToString('o')
    $to = [DateTime]::UtcNow.AddHours(1).ToString('o')
    $report = Check-Api POST '/api/v1/reports' -Token $opToken -Body @{parkingFacilityId=$facilityId;reportType='OCCUPANCY';periodStart=$from;periodEnd=$to} -Expect @(201)
    $reportId = Must-Id $report 'reporte'
    $null = Check-Api GET '/api/v1/reports' -Token $opToken
    $null = Check-Api GET '/api/v1/reports/{id:int}' -Path "/api/v1/reports/$reportId" -Token $opToken
    $null = Check-Api PUT '/api/v1/reports/{id:int}' -Path "/api/v1/reports/$reportId" -Token $opToken -Body @{parkingFacilityId=$facilityId;reportType='REVENUE';periodStart=$from;periodEnd=$to}

    # F. Sesiones: probar nueva sesion, refresh, revoke y DELETE sin afectar la principal.
    Write-Host "`n=== F. Sesiones y cambio de contrasena ===" -ForegroundColor Cyan
    $sessions0 = Check-Api GET '/api/v1/sessions' -Token $opToken
    $issued = Check-Api POST '/api/v1/sessions' -Token $opToken -Expect @(200)
    if (!$issued.refreshToken) { throw 'No se emitio refreshToken.' }
    $refreshed = Check-Api POST '/api/v1/sessions/refresh' -Body @{refreshToken=$issued.refreshToken} -Expect @(200)
    if (!$refreshed.refreshToken) { throw 'No se renovo el refreshToken.' }
    $null = Check-Api POST '/api/v1/sessions/revoke' -Token $opToken -Body @{refreshToken=$refreshed.refreshToken} -Expect @(204)
    $otherSession = Check-Api POST '/api/v1/sessions' -Token $opToken -Expect @(200)
    $sessionsAll = Check-Api GET '/api/v1/sessions' -Token $opToken
    $lastSession = @($sessionsAll | Where-Object { $null -ne $_ -and $null -ne $_.id -and $null -eq $_.revokedAt } | Sort-Object id -Descending | Select-Object -First 1)
    if ($lastSession.Count -lt 1) { throw 'No se encontro una sesion activa para revocar por ID.' }
    $null = Check-Api DELETE '/api/v1/sessions/{id:int}' -Path "/api/v1/sessions/$($lastSession[0].id)" -Token $opToken -Expect @(204)
    $null = Check-Api PUT '/api/v1/authentication/password' -Token $driverToken -Body @{currentPassword=$pass;newPassword='Test54321!'} -Expect @(200)

} catch {
    Write-Host "`n[ERROR GENERAL] $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Comparar endpoints llamados contra la lista original del backend.
    $ExpectedKeys = @(
        Get-Content $EndpointDoc | Where-Object { $_ -match '^\|\s*\d+\s*\|' } | ForEach-Object {
            $cells = $_.Split('|')
            $m = $cells[2].Trim().Trim([char]96)
            $p = $cells[3].Trim().Trim([char]96)
            "$m $p"
        }
    )
    $Missing = @($ExpectedKeys | Where-Object { -not $script:Executed.ContainsKey($_) })
    foreach ($item in $Missing) {
        $pieces = $item.Split(' ',2)
        $script:Results.Add([pscustomobject]@{Metodo=$pieces[0];Ruta=$pieces[1];HTTP=0;Resultado='NO EJECUTADO';Milisegundos=0;Detalle='No se alcanzo esta operacion (una dependencia previa fallo).'})
    }
    $csv = Join-Path $ReportDir "endpoints-$RunTag.csv"
    $json = Join-Path $ReportDir "endpoints-$RunTag.json"
    $script:Results | Export-Csv -Path $csv -NoTypeInformation -Encoding UTF8
    ConvertTo-Json -InputObject @($script:Results.ToArray()) -Depth 8 | Set-Content -Path $json -Encoding UTF8
    $successful = @($script:Results | Where-Object { $_.Resultado -eq 'OK' }).Count
    $failed = @($script:Results | Where-Object { $_.Resultado -eq 'ERROR' }).Count
    $unrun = @($script:Results | Where-Object { $_.Resultado -eq 'NO EJECUTADO' }).Count
    $unique = @($script:Executed.Keys | Where-Object { $ExpectedKeys -contains $_ }).Count
    Write-Host "`n==============================================" -ForegroundColor Cyan
    Write-Host "RESULTADO EASYPARK: $successful llamadas OK; $failed errores; $unrun rutas sin ejecutar."
    Write-Host "Cobertura: $unique / $($ExpectedKeys.Count) rutas unicas del documento."
    Write-Host "CSV:  $csv"
    Write-Host "JSON: $json"
    Write-Host 'Nota: la prueba ha creado datos nuevos en tu MySQL local.' -ForegroundColor Yellow
    if ($script:ApiOwned -and $script:ApiProcess) {
        Write-Host "La API que inicio este script sigue encendida (PID: $($script:ApiProcess.Id))." -ForegroundColor Green
    }
    if ($unique -ne 60 -or $failed -gt 0 -or $unrun -gt 0 -or $ExpectedKeys.Count -ne 60) {
        Write-Host '[RESULTADO] NO APROBADO: revisa las filas con ERROR en el CSV.' -ForegroundColor Red
    } else {
        Write-Host '[RESULTADO] APROBADO: las 60 rutas respondieron correctamente.' -ForegroundColor Green
    }
}

