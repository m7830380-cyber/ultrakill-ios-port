param(
    [string]$RipRoot = $env:ULTRAKILL_RIP,
    [string]$RetailPath = $env:ULTRAKILL_RETAIL,
    [string]$UnityProject = "$PSScriptRoot\..\unity-ultrakill",
    [switch]$SkipManagedDlls,
    [switch]$LaunchAssetRipperExport
)

$ErrorActionPreference = "Stop"
if (-not $RipRoot) {
    $RipRoot = "C:\Users\v0id\Downloads\cockadoodledo\fdadsfsadfff"
}
if (-not $RetailPath) {
    $RetailPath = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL"
}

$gameScripts = Join-Path $RipRoot "Scripts\Assembly-CSharp"
if (-not (Test-Path $gameScripts)) {
    throw "Decompiled game scripts not found at $gameScripts. Export with AssetRipper (Scripts) or run scripts/Decompile-GameScripts.ps1"
}

$managed = Join-Path $RetailPath "ULTRAKILL_Data\Managed"
if (-not (Test-Path (Join-Path $managed "Assembly-CSharp.dll"))) {
    throw "Retail Managed folder missing at $managed"
}

$UnityProject = (Resolve-Path (New-Item -ItemType Directory -Force -Path $UnityProject)).Path
$assets = Join-Path $UnityProject "Assets"
$pluginsRetail = Join-Path $assets "Plugins\RetailManaged"
$gameLink = Join-Path $assets "Game"

Write-Host "Unity port root: $UnityProject"

# Mobile + loader overlay (full LegacyInput, not engine stub)
& (Join-Path $PSScriptRoot "Sync-EngineScripts.ps1") -UnityProject $UnityProject -RipRoot $RipRoot
$iosOverlay = Join-Path $assets "UltrakillIOS"
Copy-Item (Join-Path (Split-Path $PSScriptRoot -Parent) "patches\MobileTouch\LegacyInputSynthesizer.cs") (Join-Path $iosOverlay "LegacyInputSynthesizer.cs") -Force
Copy-Item (Join-Path (Split-Path $PSScriptRoot -Parent) "patches\MobileTouch\AddressablesContentRemap.cs") (Join-Path $iosOverlay "AddressablesContentRemap.cs") -Force

# Editor / iOS build helpers from unity-ios
$engineIos = Join-Path $PSScriptRoot "..\unity-ios\Assets"
foreach ($rel in @("Editor\IosUnsignedBuild.cs", "Editor\IosDocumentSharingPostprocess.cs", "Plugins\iOS\UltrakillDocuments.mm")) {
    $src = Join-Path $engineIos $rel
    $dst = Join-Path $assets $rel
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force -Path (Split-Path $dst) | Out-Null
        Copy-Item $src $dst -Force
    }
}

# Decompiled Assembly-CSharp (dnSpy / AssetRipper / ilspycmd output)
if (Test-Path $gameLink) {
    cmd /c "rmdir `"$gameLink`" 2>nul"
    Remove-Item -Force $gameLink -ErrorAction SilentlyContinue
}
Write-Host "Linking decompiled scripts: $gameLink -> $gameScripts"
cmd /c "mklink /J `"$gameLink`" `"$gameScripts`""

if (-not $SkipManagedDlls) {
    Write-Host "Copying retail Managed DLLs (references for game scripts, excludes Assembly-CSharp)..."
    if (Test-Path $pluginsRetail) {
        Remove-Item -Recurse -Force $pluginsRetail
    }
    New-Item -ItemType Directory -Force -Path $pluginsRetail | Out-Null
    Get-ChildItem $managed -Filter "*.dll" |
        Where-Object { $_.Name -ne "Assembly-CSharp.dll" } |
        ForEach-Object { Copy-Item $_.FullName (Join-Path $pluginsRetail $_.Name) }
}

# Player defines for full port (Addressables remap compile)
$projectSettings = Join-Path $UnityProject "ProjectSettings\ProjectSettings.asset"
if (-not (Test-Path $projectSettings)) {
    @(
        '%YAML 1.1',
        '%TAG !u! tag:unity3d.com,2011:',
        '--- !u!129 &1',
        'PlayerSettings:',
        '  productName: ULTRAKILL',
        '  scriptingDefineSymbols:',
        '    iPhone: ULTRAKILL_FULL_PORT',
        '    iOS: ULTRAKILL_FULL_PORT'
    ) | Set-Content $projectSettings -Encoding UTF8
}

$editorBuild = Join-Path $UnityProject "ProjectSettings\EditorBuildSettings.asset"
@(
    '%YAML 1.1',
    '%TAG !u! tag:unity3d.com,2011:',
    '--- !u!1045 &1',
    'EditorBuildSettings:',
    '  m_ObjectHideFlags: 0',
    '  serializedVersion: 2',
    '  m_Scenes:',
    '  - enabled: 1',
    '    path: Assets/Scenes/Bootstrap.unity',
    '    guid: a1b2c3d4e5f60718293a4b5c6d7e8f90',
    '  m_configObjects: {}'
) | Set-Content $editorBuild -Encoding UTF8
Write-Host ""
Write-Host "Done. Open $UnityProject in Unity 2022.3.62f1, let it import, fix any script errors (Steam stubs etc.), then iOS build."
Write-Host "GitHub CI still builds unity-ios (shell) unless you pack this folder as a release tarball."

if ($LaunchAssetRipperExport) {
    & (Join-Path $PSScriptRoot "Export-UnityViaAssetRipper.ps1") -ExportPath $UnityProject
}
