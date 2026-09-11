$ErrorActionPreference = "Stop"

$frontendRoot = $PSScriptRoot
$serverPath = Join-Path $frontendRoot "server.js"
$nodePath = Join-Path $env:ProgramFiles "nodejs\node.exe"
$logPath = Join-Path $frontendRoot "node.log"

try {
    if (-not (Test-Path -LiteralPath $nodePath)) {
        throw "Node.js introuvable : $nodePath"
    }
    if (-not (Test-Path -LiteralPath $serverPath)) {
        throw "Serveur Next.js introuvable : $serverPath"
    }

    Set-Location -LiteralPath $frontendRoot
    $env:PORT = "3000"
    $env:HOSTNAME = "127.0.0.1"
    Add-Content -LiteralPath $logPath -Value "[$(Get-Date -Format o)] Démarrage Next.js"
    & $nodePath $serverPath >> $logPath 2>&1
    $exitCode = $LASTEXITCODE
    Add-Content -LiteralPath $logPath -Value "[$(Get-Date -Format o)] Next.js arrêté avec le code $exitCode"
    exit $exitCode
}
catch {
    Add-Content -LiteralPath $logPath -Value "[$(Get-Date -Format o)] Erreur : $($_.Exception.Message)"
    exit 1
}
