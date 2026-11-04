#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo 'Instala .NET 10 SDK'; exit 1; }
dotnet restore EasyPark.Platform.sln
dotnet build EasyPark.Platform.sln -c Release --no-restore
dotnet test EasyPark.Platform.sln -c Release --no-build
