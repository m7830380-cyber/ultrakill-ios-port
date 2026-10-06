param(
    [string]$UnityProject = "$PSScriptRoot\..\unity-ios",
    [string]$RetailManaged = $env:ULTRAKILL_MANAGED
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
if (-not $RetailManaged) {
    $RetailManaged = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\Managed"
}
if (-not (Test-Path (Join-Path $RetailManaged "Assembly-CSharp.dll"))) {
    throw "Retail Managed not found at $RetailManaged"
}

& (Join-Path $PSScriptRoot "patch-assembly.ps1") -GameManaged $RetailManaged -OutputDir (Join-Path $root "artifacts\patched")

$plugins = Join-Path $UnityProject "Assets\Plugins\UltrakillManaged"
if (Test-Path $plugins) {
    Remove-Item -Recurse -Force $plugins
}
New-Item -ItemType Directory -Force -Path $plugins | Out-Null

Get-ChildItem $RetailManaged -Filter "*.dll" | Copy-Item -Destination $plugins -Force
Copy-Item (Join-Path $root "artifacts\patched\UltrakillMobileTouch.dll") (Join-Path $plugins "UltrakillMobileTouch.dll") -Force

$ultrakillIos = Join-Path $UnityProject "Assets\UltrakillIOS"
if (Test-Path $ultrakillIos) {
    Get-ChildItem $ultrakillIos -Filter "*.cs" | Remove-Item -Force
}

$retailData = Split-Path $RetailManaged -Parent
$linkSrc = Join-Path $retailData "StreamingAssets\aa\AddressablesLink\link.xml"
if (Test-Path $linkSrc) {
    Copy-Item $linkSrc (Join-Path $UnityProject "Assets\link.xml") -Force
}

Write-Host "Staged $(@(Get-ChildItem $plugins -Filter '*.dll').Count) DLLs under $plugins"
Write-Host "Next: Package-ManagedTarball.ps1 for CI, or build unity-ios locally on a Mac."
