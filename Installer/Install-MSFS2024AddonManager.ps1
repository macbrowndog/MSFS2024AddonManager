$ErrorActionPreference = "Stop"

$applicationName = "MSFS 2024 Addons Manager"
$applicationExe = Join-Path $PSScriptRoot "MSFS2024AddonManager.exe"
$runtimeInstallerUrl = "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
$runtimeInstaller = Join-Path `
    ([IO.Path]::GetTempPath()) `
    "windowsdesktop-runtime-$([Guid]::NewGuid().ToString('N')).exe"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-DotNet10DesktopRuntime {
    $dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        return $false
    }

    $installedRuntimes = & $dotnet.Source --list-runtimes 2>$null
    return $null -ne ($installedRuntimes |
        Select-String -Pattern "^Microsoft\.WindowsDesktop\.App 10\.")
}

if (-not (Test-IsAdministrator)) {
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments
    exit 0
}

if (-not (Test-Path -LiteralPath $applicationExe)) {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        "$applicationName could not be found. Keep the installer files in the application folder.",
        $applicationName,
        "OK",
        "Error") | Out-Null
    exit 1
}

if (-not (Test-DotNet10DesktopRuntime)) {
    Add-Type -AssemblyName PresentationFramework
    $answer = [System.Windows.MessageBox]::Show(
        ".NET 10 Desktop Runtime is required and was not found. Download and install it now from Microsoft?",
        $applicationName,
        "YesNo",
        "Question")

    if ($answer -ne "Yes") {
        exit 1
    }

    try {
        Invoke-WebRequest -Uri $runtimeInstallerUrl -OutFile $runtimeInstaller

        $signature = Get-AuthenticodeSignature -FilePath $runtimeInstaller
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
            $signature.SignerCertificate.Subject -notmatch "(^|,\s*)CN=Microsoft Corporation(,|$)") {
            throw "The downloaded .NET installer does not have a valid Microsoft Authenticode signature."
        }

        $installerProcess = Start-Process $runtimeInstaller `
            -ArgumentList "/install", "/passive", "/norestart" `
            -Wait `
            -PassThru

        if ($installerProcess.ExitCode -notin 0, 3010) {
            throw ".NET installation returned exit code $($installerProcess.ExitCode)."
        }
    }
    finally {
        Remove-Item -LiteralPath $runtimeInstaller -Force -ErrorAction SilentlyContinue
    }

    if (-not (Test-DotNet10DesktopRuntime)) {
        throw ".NET 10 Desktop Runtime could not be verified after installation."
    }
}

Start-Process -FilePath $applicationExe -WorkingDirectory $PSScriptRoot
