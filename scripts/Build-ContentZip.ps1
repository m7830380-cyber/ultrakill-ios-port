param(
    [string]$RetailData = $env:ULTRAKILL_DATA,
    [string]$OutputZip = "$PSScriptRoot\..\artifacts\ULTRAKILL-Content.zip",
    [string]$RipRoot = $env:ULTRAKILL_RIP,
    [switch]$RepackOnly
)

$ErrorActionPreference = "Stop"
if (-not $RetailData) {
    $RetailData = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data"
}
if (-not (Test-Path $RetailData)) {
    throw "ULTRAKILL_Data not found at $RetailData"
}

$staging = Join-Path $env:TEMP "ultrakill-content-staging"
if (Test-Path $staging) {
    Remove-Item -Recurse -Force $staging
}
New-Item -ItemType Directory -Force -Path (Join-Path $staging "ULTRAKILL_Data") | Out-Null

Write-Host "Copying ULTRAKILL_Data from Steam (this takes a while)..."
robocopy $RetailData (Join-Path $staging "ULTRAKILL_Data") /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}

if ($RipRoot -and (Test-Path $RipRoot)) {
    Write-Host "Merging AssetRipper Scripts from $RipRoot"
    $scriptsDest = Join-Path $staging "ULTRAKILL_Data\RipScripts"
    New-Item -ItemType Directory -Force -Path $scriptsDest | Out-Null
    robocopy (Join-Path $RipRoot "Scripts") $scriptsDest /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
}

$manifest = @{
    builtUtc   = (Get-Date).ToUniversalTime().ToString("o")
    source     = $RetailData
    zipLayout  = "Extract to cache; app expects ULTRAKILL_Data at zip root"
} | ConvertTo-Json
$manifest | Set-Content (Join-Path $staging "manifest.json") -Encoding UTF8

New-Item -ItemType Directory -Force -Path (Split-Path $OutputZip) | Out-Null
if (Test-Path $OutputZip) {
    Remove-Item -Force $OutputZip
}

$OutputZip = [System.IO.Path]::GetFullPath($OutputZip)
$staging = [System.IO.Path]::GetFullPath($staging)
$winTar = Join-Path $env:SystemRoot "System32\tar.exe"
if (-not (Test-Path $winTar)) {
    throw "Windows tar not found at $winTar"
}

Write-Host "Creating $OutputZip (tar)..."
if (Test-Path $OutputZip) { Remove-Item -Force $OutputZip }
& $winTar -a -cf $OutputZip -C $staging .
if ($LASTEXITCODE -ne 0) { throw "tar failed creating content zip (exit $LASTEXITCODE)" }
Write-Host "Done: $OutputZip ($([math]::Round((Get-Item $OutputZip).Length / 1GB, 2)) GB)"
Remove-Item -Recurse -Force $staging
