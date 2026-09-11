[CmdletBinding()]
param(
    [string]$DeployRoot = "C:\Sites\Coris",
    [string]$ApiOutput = "",
    [string]$FrontendOutput = "",
    [string]$PublicApiUrl = "/api"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\.."))
$backendProject = Join-Path $repoRoot "backend\IncidentsDsi.Api.csproj"
$frontendRoot = Join-Path $repoRoot "frontend"

if ([string]::IsNullOrWhiteSpace($ApiOutput)) {
    $ApiOutput = Join-Path $DeployRoot "api"
}
if ([string]::IsNullOrWhiteSpace($FrontendOutput)) {
    $FrontendOutput = Join-Path $DeployRoot "frontend"
}

Write-Host "Publication de l'API vers $ApiOutput"
dotnet publish $backendProject --configuration Release --output $ApiOutput

Push-Location $frontendRoot
try {
    Write-Host "Installation des dépendances frontend"
    npm ci

    # NEXT_PUBLIC_API_URL est injectée dans le bundle au moment du build.
    $env:NEXT_PUBLIC_API_URL = $PublicApiUrl
    Write-Host "Build Next.js avec NEXT_PUBLIC_API_URL=$PublicApiUrl"
    npm run build
}
finally {
    Pop-Location
}

$standaloneRoot = Join-Path $frontendRoot ".next\standalone"
$staticRoot = Join-Path $frontendRoot ".next\static"
$publicRoot = Join-Path $frontendRoot "public"

if (-not (Test-Path -LiteralPath (Join-Path $standaloneRoot "server.js"))) {
    throw "Le build Next.js n'a pas produit .next\standalone\server.js."
}

if (Test-Path -LiteralPath $FrontendOutput) {
    Remove-Item -LiteralPath $FrontendOutput -Recurse -Force
}
New-Item -ItemType Directory -Path $FrontendOutput -Force | Out-Null

Copy-Item -Path (Join-Path $standaloneRoot "*") -Destination $FrontendOutput -Recurse -Force

$frontendStaticOutput = Join-Path $FrontendOutput ".next\static"
New-Item -ItemType Directory -Path $frontendStaticOutput -Force | Out-Null
Copy-Item -Path (Join-Path $staticRoot "*") -Destination $frontendStaticOutput -Recurse -Force

if (Test-Path -LiteralPath $publicRoot) {
    $frontendPublicOutput = Join-Path $FrontendOutput "public"
    New-Item -ItemType Directory -Path $frontendPublicOutput -Force | Out-Null
    Copy-Item -Path (Join-Path $publicRoot "*") -Destination $frontendPublicOutput -Recurse -Force
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "frontend-web.config") -Destination (Join-Path $FrontendOutput "web.config") -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "start-frontend.ps1") -Destination (Join-Path $FrontendOutput "start-frontend.ps1") -Force

Write-Host "Publication terminée."
Write-Host "API IIS : $ApiOutput"
Write-Host "Frontend Next.js : $FrontendOutput"
