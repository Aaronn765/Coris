[CmdletBinding()]
param(
    [int]$FrontendPort = 8080,
    [switch]$InspectOnly
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
        "-FrontendPort",
        "$FrontendPort"
    )
    if ($InspectOnly) {
        $argumentList += "-InspectOnly"
    }
    $elevated = Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $argumentList -Wait -PassThru
    $logPath = Join-Path $env:TEMP "coris-iis-finalize.log"
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath
    }
    exit $elevated.ExitCode
}

$logPath = Join-Path $env:TEMP "coris-iis-finalize.log"
Start-Transcript -Path $logPath -Force | Out-Null
trap {
    Write-Error $_
    try { Stop-Transcript | Out-Null } catch { }
    exit 1
}

$appcmd = Join-Path $env:windir "System32\inetsrv\appcmd.exe"
if (-not (Test-Path -LiteralPath $appcmd)) {
    throw "IIS est introuvable."
}

$frontendPath = "C:\Sites\Coris\frontend"
$frontendServer = Join-Path $frontendPath "server.js"
$frontendStartScript = Join-Path $frontendPath "start-frontend.ps1"
$apiPool = "CorisApiPool"
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name

if ($InspectOnly) {
    Write-Host "--- Sites IIS ---"
    & $appcmd list site
    Write-Host "--- Tâche Next.js ---"
    $task = Get-ScheduledTask -TaskName "Coris-Frontend-Next"
    $taskInfo = Get-ScheduledTaskInfo -TaskName "Coris-Frontend-Next"
    $taskAction = $task.Actions | Select-Object -First 1
    Write-Host "Executable : $($taskAction.Execute)"
    Write-Host "Arguments : $($taskAction.Arguments)"
    Write-Host "Répertoire : $($taskAction.WorkingDirectory)"
    Write-Host "État : $($task.State), dernier code : $($taskInfo.LastTaskResult)"
    Write-Host "Dernier démarrage : $($taskInfo.LastRunTime), prochaine exécution : $($taskInfo.NextRunTime)"
    Write-Host "Compte : $($task.Principal.UserId), type : $($task.Principal.LogonType)"
    Write-Host "Instances multiples : $($task.Settings.MultipleInstances)"
    Write-Host "Démarrage à la demande : $($task.Settings.AllowDemandStart)"
    Stop-Transcript | Out-Null
    exit 0
}

function Invoke-AppCmd {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & $appcmd @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "appcmd a échoué avec le code $LASTEXITCODE : $($Arguments -join ' ')"
    }
}

Write-Host "Redémarrage de l'API IIS"
Invoke-AppCmd -Arguments @("stop", "site", "/site.name:CorisApi")
Invoke-AppCmd -Arguments @("start", "site", "/site.name:CorisApi")
Invoke-AppCmd -Arguments @("recycle", "apppool", "/apppool.name:$apiPool")
Invoke-AppCmd -Arguments @("set", "site", "Coris", "/bindings:http/*:${FrontendPort}:")

Write-Host "Redémarrage de la tâche Next.js"
if (Get-ScheduledTask -TaskName "Coris-Frontend-Next" -ErrorAction SilentlyContinue) {
    & schtasks.exe /End /TN "Coris-Frontend-Next" 2>$null
    Stop-ScheduledTask -TaskName "Coris-Frontend-Next" -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName "Coris-Frontend-Next" -Confirm:$false
}
$nodePath = (Get-Command powershell.exe -ErrorAction Stop).Source
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
    -TaskName "Coris-Frontend-Next" `
    -Action $taskAction `
    -Trigger $taskTrigger `
    -Settings $taskSettings `
    -Principal $taskPrincipal | Out-Null
Start-ScheduledTask -TaskName "Coris-Frontend-Next"
$registeredTask = Get-ScheduledTask -TaskName "Coris-Frontend-Next"
$registeredAction = $registeredTask.Actions | Select-Object -First 1
Write-Host "Tâche Node : $($registeredAction.Execute) $($registeredAction.Arguments)"
Write-Host "Répertoire Node : $($registeredAction.WorkingDirectory)"

Write-Host "Attente du frontend Node.js"
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
    $taskInfo = Get-ScheduledTaskInfo -TaskName "Coris-Frontend-Next"
    throw "Next.js ne répond pas sur 127.0.0.1:3000. Dernier code de tâche : $($taskInfo.LastTaskResult)."
}

Write-Host "Attente de l'API ASP.NET Core"
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
    throw "L'API ne répond pas sur 127.0.0.1:5000 après la correction SQL."
}

Write-Host "Vérification du proxy IIS"
$frontendResponse = Invoke-WebRequest -Uri "http://127.0.0.1:${FrontendPort}/" -UseBasicParsing -TimeoutSec 5
$apiResponse = Invoke-WebRequest -Uri "http://127.0.0.1:${FrontendPort}/api/referentiels" -UseBasicParsing -TimeoutSec 5
if ($frontendResponse.StatusCode -ne 200 -or $apiResponse.StatusCode -ne 200) {
    throw "Le proxy IIS a renvoyé un statut inattendu. Frontend=$($frontendResponse.StatusCode), API=$($apiResponse.StatusCode)."
}

Write-Host "Déploiement IIS opérationnel."
Write-Host "Frontend : http://localhost:${FrontendPort}"
Write-Host "API via IIS : http://localhost:${FrontendPort}/api"
Stop-Transcript | Out-Null
