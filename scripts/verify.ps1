$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw 'Instala el SDK de .NET 10 antes de continuar.' }
dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'No se pudo ejecutar dotnet.' }
dotnet restore EasyPark.Platform.sln
if ($LASTEXITCODE -ne 0) { throw 'Falló restore.' }
dotnet build EasyPark.Platform.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Falló build.' }
dotnet test EasyPark.Platform.sln -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas.' }
Write-Host 'Compilación y pruebas correctas.' -ForegroundColor Green
