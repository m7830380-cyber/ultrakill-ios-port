param(
    [string]$IosContent = "$PSScriptRoot\..\artifacts\ios-content\ULTRAKILL_Data",
    [string]$RetailData = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data",
    [string]$OutputRoot = "$PSScriptRoot\..\artifacts\ULTRAKILL-Content-merged"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $IosContent)) {
    throw "Run Build-IosBundles.ps1 first — missing $IosContent"
}
if (-not (Test-Path $RetailData)) {
    throw "Retail ULTRAKILL_Data not found at $RetailData"
}

$dest = Join-Path $OutputRoot "ULTRAKILL_Data"
if (Test-Path $OutputRoot) {
    Remove-Item -Recurse -Force $OutputRoot
}
New-Item -ItemType Directory -Force -Path $dest | Out-Null

Write-Host "Copying retail ULTRAKILL_Data..."
robocopy $RetailData $dest /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy retail failed $LASTEXITCODE" }

$iosAa = Join-Path $IosContent "StreamingAssets\aa"
$destAa = Join-Path $dest "StreamingAssets\aa"
Write-Host "Overlay iOS rebuilt bundles from $iosAa"
robocopy $iosAa $destAa /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy ios aa failed $LASTEXITCODE" }

Write-Host "Merged content at $OutputRoot"
Write-Host "Copy ULTRAKILL_Data to iPhone: Documents/ULTRAKILL-Content/ULTRAKILL_Data"
