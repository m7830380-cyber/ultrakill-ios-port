param(
    [string]$Bundle = "$PSScriptRoot\..\artifacts\ios-content\ULTRAKILL_Data\StreamingAssets\aa\iOS\specialscenes_scenes_tutorial.bundle",
    [long]$MinBytes = 7500000
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $Bundle)) {
    throw "Missing $Bundle — run scripts/Build-IosBundles.ps1 first."
}

$bytes = [System.IO.File]::ReadAllBytes($Bundle)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
$markers = @(
    "StaticSceneOptimizer",
    "Combined Mesh"
)
Write-Host "Tutorial iOS bundle: $Bundle"
Write-Host "  size=$($bytes.Length) minExpected=$MinBytes"
foreach ($m in $markers) {
    $found = $ascii.Contains($m)
    Write-Host "  marker '$m' = $found"
}
if ($bytes.Length -lt $MinBytes) {
    throw "Bundle too small — bake data likely still missing."
}
foreach ($m in $markers) {
    if (-not $ascii.Contains($m)) {
        throw "Missing marker: $m"
    }
}
Write-Host "OK: Tutorial bundle looks like retail bake payload."
