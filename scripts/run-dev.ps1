
Write-Host "== Run Dev =="
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

dotnet build -c Debug

Write-Host "[INFO] Launching Dashboard (WPF)."
dotnet run --project .\apps\Dashboard.Win\Dashboard.Win.csproj
Pop-Location
