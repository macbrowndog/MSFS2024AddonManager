[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern("^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$")]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$OutputDirectory,

    [string]$RepositoryRoot = (Join-Path $PSScriptRoot "..\.."),

    [string]$LegalNoticesRoot,

    [string]$FrameworkLauncherScript,

    [string]$CertificatePath,

    [string]$CertificatePassword,

    [ValidatePattern("^https://")]
    [string]$TimestampUrl = "https://timestamp.digicert.com"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
$projectPath = Join-Path `
    $repositoryRoot `
    "MSFS2024AddonManager\MSFS2024AddonManager.csproj"

if (Test-Path -LiteralPath $outputPath) {
    throw "Output directory already exists: $outputPath"
}

if ([string]::IsNullOrWhiteSpace($LegalNoticesRoot)) {
    $LegalNoticesRoot = $repositoryRoot
}

$legalNoticesRoot = [IO.Path]::GetFullPath($LegalNoticesRoot)
$licensePath = Join-Path $legalNoticesRoot "LICENSE"
$thirdPartyNoticesPath = Join-Path $legalNoticesRoot "THIRD-PARTY-NOTICES.md"
foreach ($requiredNotice in @($licensePath, $thirdPartyNoticesPath)) {
    if (-not (Test-Path -LiteralPath $requiredNotice -PathType Leaf)) {
        throw "Required legal notice was not found: $requiredNotice"
    }
}

$dotnetCommand = Get-Command dotnet -ErrorAction Stop
$dotnetRoot = Split-Path -Parent $dotnetCommand.Source
$dotnetLicensePath = Join-Path $dotnetRoot "LICENSE.txt"
$dotnetThirdPartyNoticesPath = Join-Path $dotnetRoot "ThirdPartyNotices.txt"
foreach ($requiredNotice in @($dotnetLicensePath, $dotnetThirdPartyNoticesPath)) {
    if (-not (Test-Path -LiteralPath $requiredNotice -PathType Leaf)) {
        throw "Required .NET redistribution notice was not found: $requiredNotice"
    }
}

$stagingPath = Join-Path $outputPath "staging"
$releasePath = Join-Path $outputPath "release"
$selfContainedName = "MSFS2024AddonManager-$Version-win-x64-self-contained"
$frameworkDependentName = "MSFS2024AddonManager-$Version-win-x64-framework-dependent"
$selfContainedPath = Join-Path $stagingPath $selfContainedName
$frameworkDependentPath = Join-Path $stagingPath $frameworkDependentName

New-Item -ItemType Directory -Path $selfContainedPath -Force | Out-Null
New-Item -ItemType Directory -Path $frameworkDependentPath -Force | Out-Null
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    -p:PublishDir="$selfContainedPath\" `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugSymbols=false `
    -p:DebugType=None `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:ContinuousIntegrationBuild=true

if ($LASTEXITCODE -ne 0) {
    throw "The self-contained publish failed."
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --no-restore `
    -p:PublishDir="$frameworkDependentPath\" `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugSymbols=false `
    -p:DebugType=None `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:ContinuousIntegrationBuild=true

if ($LASTEXITCODE -ne 0) {
    throw "The framework-dependent publish failed."
}

$installerPath = Join-Path $repositoryRoot "Installer"
$releaseNotesPath = Join-Path $installerPath "RELEASE-NOTES-$Version.txt"
if (-not (Test-Path -LiteralPath $releaseNotesPath -PathType Leaf)) {
    throw "Release notes were not found: $releaseNotesPath"
}

if ([string]::IsNullOrWhiteSpace($FrameworkLauncherScript)) {
    $FrameworkLauncherScript = Join-Path `
        $installerPath `
        "Install-MSFS2024AddonManager.ps1"
}

if (-not (Test-Path -LiteralPath $FrameworkLauncherScript -PathType Leaf)) {
    throw "Framework-dependent launcher was not found: $FrameworkLauncherScript"
}

Copy-Item (Join-Path $installerPath "README.txt") $selfContainedPath
Copy-Item (Join-Path $installerPath "README.txt") $frameworkDependentPath
Copy-Item (Join-Path $installerPath "Install and Run.cmd") $frameworkDependentPath
Copy-Item $FrameworkLauncherScript $frameworkDependentPath
Copy-Item $releaseNotesPath (Join-Path $selfContainedPath "RELEASE-NOTES.txt")
Copy-Item $releaseNotesPath (Join-Path $frameworkDependentPath "RELEASE-NOTES.txt")
Copy-Item $licensePath $selfContainedPath
Copy-Item $licensePath $frameworkDependentPath
Copy-Item $thirdPartyNoticesPath $selfContainedPath
Copy-Item $thirdPartyNoticesPath $frameworkDependentPath
Copy-Item $dotnetLicensePath (Join-Path $selfContainedPath "DOTNET-LICENSE.txt")
Copy-Item $dotnetLicensePath (Join-Path $frameworkDependentPath "DOTNET-LICENSE.txt")
Copy-Item `
    $dotnetThirdPartyNoticesPath `
    (Join-Path $selfContainedPath "DOTNET-THIRD-PARTY-NOTICES.txt")
Copy-Item `
    $dotnetThirdPartyNoticesPath `
    (Join-Path $frameworkDependentPath "DOTNET-THIRD-PARTY-NOTICES.txt")
Copy-Item $releaseNotesPath $releasePath

$executables = @(
    Join-Path $selfContainedPath "MSFS2024AddonManager.exe"
    Join-Path $frameworkDependentPath "MSFS2024AddonManager.exe"
)

foreach ($executable in $executables) {
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw "Published executable was not found: $executable"
    }
}

$isSigned = -not [string]::IsNullOrWhiteSpace($CertificatePath)
if ($isSigned) {
    if (-not (Test-Path -LiteralPath $CertificatePath -PathType Leaf)) {
        throw "Signing certificate was not found: $CertificatePath"
    }

    $signTool = Get-ChildItem `
        -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
        -Filter "signtool.exe" `
        -File `
        -Recurse |
        Where-Object { $_.DirectoryName -match "\\x64$" } |
        Sort-Object {
            if ($_.FullName -match "\\bin\\(?<version>\d+\.\d+\.\d+\.\d+)\\") {
                return [version]$Matches.version
            }

            return [version]"0.0"
        } -Descending |
        Select-Object -First 1

    if ($null -eq $signTool) {
        throw "SignTool was not found in the Windows SDK."
    }

    foreach ($executable in $executables) {
        $signArguments = @(
            "sign",
            "/f", $CertificatePath,
            "/fd", "SHA256",
            "/tr", $TimestampUrl,
            "/td", "SHA256"
        )

        if (-not [string]::IsNullOrEmpty($CertificatePassword)) {
            $signArguments += @("/p", $CertificatePassword)
        }

        $signArguments += $executable
        & $signTool.FullName $signArguments
        if ($LASTEXITCODE -ne 0) {
            throw "Authenticode signing failed for '$executable'."
        }

        & $signTool.FullName verify /pa /v $executable
        if ($LASTEXITCODE -ne 0) {
            throw "Authenticode verification failed for '$executable'."
        }
    }
}

$signingMessage = if ($isSigned) {
    "The application executable is Authenticode-signed and RFC 3161 timestamped."
}
else {
    "The application executable is unsigned. Verify this archive against SHA256SUMS.txt from the GitHub Release."
}

Set-Content `
    -LiteralPath (Join-Path $selfContainedPath "SIGNING.txt") `
    -Value $signingMessage `
    -Encoding utf8
Set-Content `
    -LiteralPath (Join-Path $frameworkDependentPath "SIGNING.txt") `
    -Value $signingMessage `
    -Encoding utf8

$selfContainedArchive = Join-Path $releasePath "$selfContainedName.zip"
$frameworkDependentArchive = Join-Path $releasePath "$frameworkDependentName.zip"
Compress-Archive `
    -Path (Join-Path $selfContainedPath "*") `
    -DestinationPath $selfContainedArchive `
    -CompressionLevel Optimal
Compress-Archive `
    -Path (Join-Path $frameworkDependentPath "*") `
    -DestinationPath $frameworkDependentArchive `
    -CompressionLevel Optimal

$checksumLines = Get-ChildItem -LiteralPath $releasePath -Filter "*.zip" |
    Sort-Object Name |
    ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash *$($_.Name)"
    }

Set-Content `
    -LiteralPath (Join-Path $releasePath "SHA256SUMS.txt") `
    -Value $checksumLines `
    -Encoding ascii

Write-Output "Release artifacts created in $releasePath"
