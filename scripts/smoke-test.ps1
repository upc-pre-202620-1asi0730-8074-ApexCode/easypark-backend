param(
  [string]$BaseUrl = 'http://localhost:8080',
  [string]$Email = 'operador.demo@easypark.local',
  [string]$Password = 'Demo12345!'
)
$ErrorActionPreference = 'Stop'
$ready = Invoke-RestMethod "$BaseUrl/health/ready"
if ($ready.status -ne 'ready') { throw 'La base de datos no está lista.' }
$body = @{ email = $Email; password = $Password } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post "$BaseUrl/api/v1/authentication/sign-in" -ContentType 'application/json' -Body $body
if (-not $login.token -or -not $login.refreshToken) { throw 'El login no devolvió ambos tokens.' }
$header = @{ Authorization = "Bearer $($login.token)" }
$sessions = Invoke-RestMethod -Headers $header "$BaseUrl/api/v1/sessions"
$rules = Invoke-RestMethod -Headers $header "$BaseUrl/api/v1/alert-rules"
$alerts = Invoke-RestMethod -Headers $header "$BaseUrl/api/v1/alerts"
$templates = Invoke-RestMethod -Headers $header "$BaseUrl/api/v1/notification-templates"
$metrics = Invoke-RestMethod -Headers $header "$BaseUrl/api/v1/occupancy-metrics"
$reports = Invoke-RestMethod -Headers $header "$BaseUrl/api/v1/reports"
$renew = Invoke-RestMethod -Method Post "$BaseUrl/api/v1/sessions/refresh" -ContentType 'application/json' -Body (@{refreshToken=$login.refreshToken} | ConvertTo-Json)
if (-not $renew.token -or -not $renew.refreshToken) { throw 'Falló la rotación de refresh tokens.' }
Write-Host 'Smoke test REST correcto. Consultas y renovación de sesión disponibles.' -ForegroundColor Green
