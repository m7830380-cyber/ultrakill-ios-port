param(
    [string]$GameData = $env:ULTRAKILL_DATA,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
if (-not $GameData) {
    $GameData = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data"
}
$managed = Join-Path $GameData "Managed"
if (-not (Test-Path $managed)) {
    throw "Managed folder not found: $managed"
}

$root = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot "patch-assembly.ps1") -GameManaged $managed -OutputDir (Join-Path $root "artifacts\patched")

$patchDll = Join-Path $root "artifacts\patched\UltrakillMobileTouch.dll"
$scriptingPatch = Join-Path $root "artifacts\patched\ScriptingAssemblies.json"
$destDll = Join-Path $managed "UltrakillMobileTouch.dll"
$destScripting = Join-Path $GameData "ScriptingAssemblies.json"

if ($WhatIf) {
    Write-Host "Would copy $patchDll -> $destDll"
    Write-Host "Would merge $scriptingPatch -> $destScripting"
    exit 0
}

Copy-Item $patchDll $destDll -Force
$json = Get-Content $scriptingPatch -Raw | ConvertFrom-Json
$live = Get-Content $destScripting -Raw | ConvertFrom-Json
foreach ($name in $json.names) {
    if ($live.names -notcontains $name) {
        $live.names += $name
        $live.types += $json.types[[array]::IndexOf($json.names, $name)]
    }
}
$live | ConvertTo-Json -Compress | Set-Content $destScripting -Encoding UTF8
Write-Host "PC touch sidecar installed. Launch ULTRAKILL.exe to verify overlay."
