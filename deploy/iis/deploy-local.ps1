[CmdletBinding()]
param(
    [string]$DeployRoot = "C:\Sites\Coris",
    [int]$FrontendPort = 8080
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    Write-Host "Une élévation administrateur est requise. Validation de la demande UAC..."
    $argumentList = @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        "`"$PSCommandPath`"",
        "-DeployRoot",
        "`"$DeployRoot`"",
        "-FrontendPort",
        "$FrontendPort"
    )
    $elevated = Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $argumentList -Wait -PassThru
    $logPath = Join-Path $env:TEMP "coris-iis-deploy.log"
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath
    }
    exit $elevated.ExitCode
}

$logPath = Join-Path $env:TEMP "coris-iis-deploy.log"
Start-Transcript -Path $logPath -Force | Out-Null
trap {
    Write-Error $_
    try { Stop-Transcript | Out-Null } catch { }
    exit 1
}

$appcmd = Join-Path $env:windir "System32\inetsrv\appcmd.exe"
if (-not (Test-Path -LiteralPath $appcmd)) {
    throw "IIS est introuvable. Exécuter install-prerequisites.ps1 avant ce script."
}

$scriptRoot = $PSScriptRoot
$publishScript = Join-Path $scriptRoot "publish.ps1"
$apiPath = Join-Path $DeployRoot "api"
$frontendPath = Join-Path $DeployRoot "frontend"
$frontendServer = Join-Path $frontendPath "server.js"
$apiSite = "CorisApi"
$frontendSite = "Coris"
$apiPool = "CorisApiPool"
$frontendPool = "CorisFrontendPool"
$frontendTask = "Coris-Frontend-Next"

function Invoke-AppCmd {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & $appcmd @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "appcmd a échoué avec le code $LASTEXITCODE : $($Arguments -join ' ')"
    }
}

function Assert-IisNameAvailable {
    param([Parameter(Mandatory)][string]$Kind, [Parameter(Mandatory)][string]$Name)

    $result = & $appcmd list $Kind "/name:$Name" 2>$null
    if ($LASTEXITCODE -eq 0 -and $result) {
        throw "L'élément IIS '$Name' existe déjà. Aucun élément existant n'a été écrasé."
    }
}

if (Get-ScheduledTask -TaskName $frontendTask -ErrorAction SilentlyContinue) {
    throw "La tâche planifiée '$frontendTask' existe déjà. Aucun élément existant n'a été écrasé."
}

Assert-IisNameAvailable -Kind "site" -Name $apiSite
Assert-IisNameAvailable -Kind "site" -Name $frontendSite
Assert-IisNameAvailable -Kind "apppool" -Name $apiPool
Assert-IisNameAvailable -Kind "apppool" -Name $frontendPool

Write-Host "Publication de l'application vers $DeployRoot"
& $publishScript -DeployRoot $DeployRoot -PublicApiUrl "/api"
if ($LASTEXITCODE -ne 0) {
    throw "La publication de l'application a échoué."
}

if (-not (Test-Path -LiteralPath (Join-Path $apiPath "web.config"))) {
    throw "web.config API absent après publication."
}
if (-not (Test-Path -LiteralPath (Join-Path $frontendPath "server.js"))) {
    throw "server.js frontend absent après publication."
}
if (-not (Test-Path -LiteralPath (Join-Path $frontendPath "start-frontend.ps1"))) {
    throw "start-frontend.ps1 frontend absent après publication."
}

Write-Host "Création des pools IIS"
Invoke-AppCmd -Arguments @("add", "apppool", "/name:$apiPool")
Invoke-AppCmd -Arguments @("set", "apppool", $apiPool, "/managedRuntimeVersion:")
Invoke-AppCmd -Arguments @("add", "apppool", "/name:$frontendPool")
Invoke-AppCmd -Arguments @("set", "apppool", $frontendPool, "/managedRuntimeVersion:")

Write-Host "Création des sites IIS"
Invoke-AppCmd -Arguments @("add", "site", "/name:$apiSite", "/physicalPath:$apiPath", "/bindings:http/127.0.0.1:5000:")
Invoke-AppCmd -Arguments @("set", "app", "$apiSite/", "/applicationPool:$apiPool")
Invoke-AppCmd -Arguments @("add", "site", "/name:$frontendSite", "/physicalPath:$frontendPath", "/bindings:http/*:${FrontendPort}:")
Invoke-AppCmd -Arguments @("set", "app", "$frontendSite/", "/applicationPool:$frontendPool")

Write-Host "Activation du proxy ARR"
Invoke-AppCmd -Arguments @("set", "config", "-section:system.webServer/proxy", "/enabled:true")

$nodePath = (Get-Command powershell.exe -ErrorAction Stop).Source
$frontendStartScript = Join-Path $frontendPath "start-frontend.ps1"
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
Write-Host "Création de la tâche planifiée $frontendTask"
$taskAction = New-ScheduledTaskAction `
    -Execute $nodePath `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$frontendStartScript`"" `
    -WorkingDirectory $frontendPath
$taskTrigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
$taskSettings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -ExecutionTimeLimit ([TimeSpan]::Zero)
$taskPrincipal = New-ScheduledTaskPrincipal `
    -UserId $currentUser `
    -LogonType Interactive `
    -RunLevel Highest
Register-ScheduledTask `
    -TaskName $frontendTask `
    -Action $taskAction `
    -Trigger $taskTrigger `
    -Settings $taskSettings `
    -Principal $taskPrincipal | Out-Null
Start-ScheduledTask -TaskName $frontendTask

Invoke-AppCmd -Arguments @("start", "site", "/site.name:$apiSite")
Invoke-AppCmd -Arguments @("start", "site", "/site.name:$frontendSite")

Write-Host "Vérification du frontend Node.js"
$frontendReady = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try {
        Invoke-WebRequest -Uri "http://127.0.0.1:3000/" -UseBasicParsing -TimeoutSec 2 | Out-Null
        $frontendReady = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}
if (-not $frontendReady) {
    throw "Next.js n'écoute pas sur 127.0.0.1:3000."
}

Write-Host "Vérification de l'API"
$apiReady = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try {
        Invoke-WebRequest -Uri "http://127.0.0.1:5000/swagger/index.html" -UseBasicParsing -TimeoutSec 2 | Out-Null
        $apiReady = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}
if (-not $apiReady) {
    throw "L'API ne répond pas sur 127.0.0.1:5000. Vérifier SQL Server et les journaux IIS."
}

Write-Host "Déploiement IIS terminé."
Write-Host "Frontend : http://localhost:$FrontendPort"
Write-Host "API interne : http://127.0.0.1:5000"
Stop-Transcript | Out-Null
