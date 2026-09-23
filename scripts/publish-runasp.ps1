# Builds the Angular app and publishes ErpClink.Api for RunASP.NET / MonsterASP.
# Output: artifacts/runasp
# Does not upload anything and does not put secrets in source control.
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$webDir = Join-Path $repoRoot "frontend\erpclink-web"
$envFile = Join-Path $webDir "src\environments\environment.ts"
$envBackup = Join-Path $env:TEMP "erpclink-environment.ts.bak"
$publishDir = Join-Path $repoRoot "artifacts\runasp"
$browserDir = Join-Path $webDir "dist\erpclink-web\browser"
$template = Join-Path $repoRoot "src\Host\ErpClink.Api\appsettings.Production.template.json"

Copy-Item $envFile $envBackup -Force
try {
    Write-Host "Building Angular (same-origin API, apiBaseUrl empty)..."
    Push-Location $webDir
    $env:API_BASE_URL = ""
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    npm run build:prod
    if ($LASTEXITCODE -ne 0) { throw "Angular production build failed" }
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
    Copy-Item $envBackup $envFile -Force
    Remove-Item $envBackup -Force -ErrorAction SilentlyContinue
    Remove-Item Env:API_BASE_URL -ErrorAction SilentlyContinue
}

if (-not (Test-Path (Join-Path $browserDir "index.html"))) {
    throw "Angular output missing: $browserDir\index.html"
}

Write-Host "Publishing ASP.NET Core (net9.0, framework-dependent)..."
dotnet publish (Join-Path $repoRoot "src\Host\ErpClink.Api\ErpClink.Api.csproj") `
    -c Release `
    -o $publishDir `
    --self-contained false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$wwwroot = Join-Path $publishDir "wwwroot"
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item (Join-Path $browserDir "*") $wwwroot -Recurse -Force
Copy-Item $template (Join-Path $publishDir "appsettings.Production.json") -Force

Write-Host ""
Write-Host "Publish folder: $publishDir"
Write-Host "Upload the CONTENTS of that folder to the RunASP site root (their wwwroot)."
Write-Host "Set ConnectionStrings__DefaultConnection and Authentication__Jwt__SigningKey in the hosting panel before the first start."
