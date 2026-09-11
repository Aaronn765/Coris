[CmdletBinding()]
param()

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
        "`"$PSCommandPath`""
    )
    $elevated = Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $argumentList -Wait -PassThru
    exit $elevated.ExitCode
}

Write-Host "Installation des prérequis IIS Coris en mode administrateur."

if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
    Install-WindowsFeature -Name Web-Server,Web-Mgmt-Console -IncludeManagementTools
}
else {
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementConsole -All -NoRestart
}

$downloadRoot = Join-Path $env:TEMP "coris-iis-prerequisites"
New-Item -ItemType Directory -Path $downloadRoot -Force | Out-Null

$packages = @(
    @{
        Name = "URL Rewrite 2.1"
        Url = "https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi"
        FileName = "rewrite_amd64_en-US.msi"
        Arguments = @("/i", "{0}", "/qn", "/norestart")
    },
    @{
        Name = "Application Request Routing 3.0"
        Url = "https://download.microsoft.com/download/E/9/8/E9849D6A-020E-47E4-9FD0-A023E99B54EB/requestRouter_amd64.msi"
        FileName = "requestRouter_amd64.msi"
        Arguments = @("/i", "{0}", "/qn", "/norestart")
    },
    @{
        Name = ".NET 10 Hosting Bundle"
        Url = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/10.0.11/dotnet-hosting-10.0.11-win.exe"
        FileName = "dotnet-hosting-10.0.11-win.exe"
        Arguments = @("/install", "/quiet", "/norestart")
    }
)

foreach ($package in $packages) {
    $installerPath = Join-Path $downloadRoot $package.FileName
    if (-not (Test-Path -LiteralPath $installerPath)) {
        Write-Host "Téléchargement : $($package.Name)"
        Invoke-WebRequest -Uri $package.Url -OutFile $installerPath
    }

    $signature = Get-AuthenticodeSignature -LiteralPath $installerPath
    if ($signature.Status -ne "Valid") {
        throw "La signature de $($package.FileName) n'est pas valide : $($signature.Status)."
    }

    Write-Host "Installation : $($package.Name)"
    if ($package.FileName.EndsWith(".msi")) {
        $arguments = $package.Arguments | ForEach-Object { $_ -replace "\{0\}", "`"$installerPath`"" }
        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $arguments -Wait -PassThru
    }
    else {
        $process = Start-Process -FilePath $installerPath -ArgumentList $package.Arguments -Wait -PassThru
    }

    if ($process.ExitCode -notin @(0, 3010)) {
        throw "Installation échouée pour $($package.Name), code $($process.ExitCode)."
    }
}

$appcmd = Join-Path $env:windir "System32\inetsrv\appcmd.exe"
if (-not (Test-Path -LiteralPath $appcmd)) {
    throw "appcmd.exe est introuvable après l'installation d'IIS."
}

Write-Host "Activation du proxy ARR au niveau serveur."
& $appcmd set config -section:system.webServer/proxy /enabled:true
if ($LASTEXITCODE -ne 0) {
    throw "Impossible d'activer le proxy ARR."
}

Write-Host "Redémarrage des services IIS."
& iisreset /restart
if ($LASTEXITCODE -ne 0) {
    throw "Le redémarrage IIS a échoué."
}

Write-Host "Prérequis IIS installés avec succès."
