param(
    [string]$UnityProject = "$PSScriptRoot\..\unity-ios",
    [string]$RipRoot = "C:\Users\v0id\Downloads\cockadoodledo\fdadsfsadfff"
)

$ErrorActionPreference = "Stop"
$src = Join-Path (Split-Path $PSScriptRoot -Parent) "patches\MobileTouch"
$dest = Join-Path $UnityProject "Assets\UltrakillIOS"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Get-ChildItem $src -Filter "*.cs" |
    Where-Object { $_.Name -notmatch '\.Engine\.cs$' -and $_.Name -ne 'LegacyInputSynthesizer.cs' } |
    Copy-Item -Destination $dest -Force
Copy-Item (Join-Path $src "LegacyInputSynthesizer.Engine.cs") (Join-Path $dest "LegacyInputSynthesizer.cs") -Force

if (Test-Path $RipRoot) {
    $ripScripts = Join-Path $UnityProject "Assets\RipScripts"
    if (Test-Path $ripScripts) {
        Remove-Item -Recurse -Force $ripScripts
    }
    Write-Host "Linking rip Scripts (junction)..."
    cmd /c "mklink /J `"$ripScripts`" `"$(Join-Path $RipRoot 'Scripts')`""
}

Write-Host "Engine scripts synced to $UnityProject"
