param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repoRoot "src\\GhostCapture.App\\GhostCapture.App.csproj"
$installerScript = Join-Path $repoRoot "installer\\GhostCapture.iss"
$publishDirectory = Join-Path $repoRoot "artifacts\\publish\\$RuntimeIdentifier"
$installerOutputDirectory = Join-Path $repoRoot "artifacts\\installer"
$toolBundleDirectory = Join-Path $repoRoot "tools\\scrcpy"
$runtimeFiles = @(
    "adb.exe",
    "AdbWinApi.dll",
    "AdbWinUsbApi.dll",
    "avcodec-61.dll",
    "avformat-61.dll",
    "avutil-59.dll",
    "icon.png",
    "libusb-1.0.dll",
    "scrcpy-server",
    "scrcpy-noconsole.vbs",
    "scrcpy.exe",
    "SDL2.dll",
    "swresample-5.dll"
)

New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $installerOutputDirectory | Out-Null

Get-ChildItem -Path $publishDirectory -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Get-ChildItem -Path $installerOutputDirectory -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force

[xml]$projectXml = Get-Content $projectPath
$appVersion = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($appVersion)) {
    $appVersion = "0.1.0"
}

dotnet publish $projectPath `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

foreach ($runtimeFile in $runtimeFiles) {
    $sourceFile = Join-Path $toolBundleDirectory $runtimeFile
    if (-not (Test-Path $sourceFile)) {
        throw "Missing runtime file: $sourceFile"
    }

    Copy-Item -Path $sourceFile -Destination $publishDirectory -Force
}

$isccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) {
    $isccCommand.Source
} else {
    Join-Path $env:LOCALAPPDATA "Programs\\Inno Setup 6\\ISCC.exe"
}

if (-not (Test-Path $isccPath)) {
    throw "Unable to locate ISCC.exe"
}

& $isccPath "/DSourceDir=$publishDirectory" "/DOutputDir=$installerOutputDirectory" "/DAppVersion=$appVersion" $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed."
}

Write-Host "Installer created in $installerOutputDirectory"
