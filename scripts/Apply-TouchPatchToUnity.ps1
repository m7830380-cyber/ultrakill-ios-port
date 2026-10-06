param(
    [string]$UnityProject = "$PSScriptRoot\..\unity-ultrakill",
    [string]$RetailManaged = $env:ULTRAKILL_MANAGED
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
if (-not $RetailManaged) {
    $RetailManaged = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\Managed"
}

$touchDest = Join-Path $UnityProject "Assets\UltrakillIOS"
New-Item -ItemType Directory -Force -Path $touchDest | Out-Null
Get-ChildItem (Join-Path $root "patches\MobileTouch\*.cs") | Copy-Item -Destination $touchDest -Force

$editorDest = Join-Path $UnityProject "Assets\Editor"
New-Item -ItemType Directory -Force -Path $editorDest | Out-Null
Copy-Item (Join-Path $root "unity-ios\Assets\Editor\IosUnsignedBuild.cs") -Destination $editorDest -Force

& (Join-Path $PSScriptRoot "patch-assembly.ps1") -GameManaged $RetailManaged -OutputDir (Join-Path $root "artifacts\patched")

$plugins = Join-Path $UnityProject "Assets\Plugins"
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
Copy-Item (Join-Path $root "artifacts\patched\UltrakillMobileTouch.dll") -Destination $plugins -Force

Write-Host "Touch patch applied under $UnityProject"
