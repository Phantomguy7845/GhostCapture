param(
    [string]$SourceDirectory = "",
    [string]$TargetDirectory = ""
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($SourceDirectory)) {
    $SourceDirectory = Join-Path $repoRoot "scrcpy-win64-v3.3.4"
}

if ([string]::IsNullOrWhiteSpace($TargetDirectory)) {
    $TargetDirectory = Join-Path $repoRoot "tools\\scrcpy"
}

if (-not (Test-Path $SourceDirectory)) {
    throw "Source directory not found: $SourceDirectory"
}

New-Item -ItemType Directory -Force -Path $TargetDirectory | Out-Null
Copy-Item -Path (Join-Path $SourceDirectory "*") -Destination $TargetDirectory -Recurse -Force

Write-Host "Synced scrcpy tool bundle"
Write-Host "  Source: $SourceDirectory"
Write-Host "  Target: $TargetDirectory"
