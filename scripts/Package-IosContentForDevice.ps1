param(
    [string]$RetailData = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data",
    [string]$IosOverlay = "$PSScriptRoot\..\artifacts\ios-content\ULTRAKILL_Data",
    [string]$OutputZip = "$env:USERPROFILE\Downloads\ULTRAKILL-Content-for-iPhone.zip",
    [string]$StagingRoot = "",
    # overlay = only files built under artifacts/ios-content (safe drop onto existing phone folder)
    # merged  = full Steam ULTRAKILL_Data + iOS aa overlay (catalog/settings/iOS/*.bundle)
    [ValidateSet("overlay", "merged")]
    [string]$Mode = "overlay",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

function Write-FileList([string]$ContentRoot) {
    $root = [IO.Path]::GetFullPath($ContentRoot).TrimEnd('\') + '\'
    $lines = Get-ChildItem $ContentRoot -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($root.Length).Replace('\', '/')
        "{0}`t{1}" -f $_.Length, $rel
    }
    [IO.File]::WriteAllLines((Join-Path $ContentRoot "filelist.txt"), $lines)
    Write-Host "filelist.txt: $($lines.Count) files under $ContentRoot"
}

function Stage-Overlay([string]$StageContentRoot) {
    if (-not (Test-Path $IosOverlay)) {
        throw "Missing iOS overlay at $IosOverlay. Run scripts/Build-IosShaders.ps1 and/or Build-IosBundles.ps1 first."
    }
    $srcAa = Join-Path $IosOverlay "StreamingAssets\aa"
    if (-not (Test-Path $srcAa)) {
        throw "No StreamingAssets/aa in overlay: $IosOverlay"
    }
    $destAa = Join-Path $StageContentRoot "ULTRAKILL_Data\StreamingAssets\aa"
    New-Item -ItemType Directory -Force -Path $destAa | Out-Null
    Write-Host "Staging overlay aa from $srcAa"
    robocopy $srcAa $destAa /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy overlay failed $LASTEXITCODE" }
}

function Stage-Merged([string]$StageContentRoot) {
    if (-not (Test-Path $RetailData)) {
        throw "Retail ULTRAKILL_Data not found at $RetailData"
    }
    $destData = Join-Path $StageContentRoot "ULTRAKILL_Data"
    Write-Host "Copying retail ULTRAKILL_Data (~3.5 GB)..."
    robocopy $RetailData $destData /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy retail failed $LASTEXITCODE" }
    if (Test-Path $IosOverlay) {
        $srcAa = Join-Path $IosOverlay "StreamingAssets\aa"
        $destAa = Join-Path $destData "StreamingAssets\aa"
        if (Test-Path $srcAa) {
            Write-Host "Overlay iOS rebuilt aa on top of retail..."
            robocopy $srcAa $destAa /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "robocopy ios aa failed $LASTEXITCODE" }
        }
        else {
            Write-Warning "No ios overlay aa at $srcAa; zip will have Windows Addressables only (wrong for iPhone)."
        }
    }
}

if (-not $StagingRoot) {
    $StagingRoot = Join-Path $env:TEMP ("ultrakill-ios-content-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
}
if (Test-Path $StagingRoot) {
    Remove-Item -Recurse -Force $StagingRoot
}
New-Item -ItemType Directory -Force -Path $StagingRoot | Out-Null

if ($Mode -eq "overlay") {
    Stage-Overlay $StagingRoot
}
else {
    Stage-Merged $StagingRoot
}

$manifest = @{
    builtUtc   = (Get-Date).ToUniversalTime().ToString("o")
    mode       = $Mode
    iosOverlay = (Resolve-Path $IosOverlay -ErrorAction SilentlyContinue).Path
    retail     = $RetailData
    phonePath  = "Files → On My iPhone → ULTRAKILL → ULTRAKILL-Content/"
    layout     = "Unpack so ULTRAKILL_Data is at Documents/ULTRAKILL-Content/ULTRAKILL_Data (merge overlay files over your existing folder)."
} | ConvertTo-Json -Depth 4
$manifest | Set-Content (Join-Path $StagingRoot "manifest.json") -Encoding UTF8

Write-FileList $StagingRoot

$readme = @"
ULTRAKILL iOS external content ($Mode)
=====================================

1. On iPhone: Files → On My iPhone → ULTRAKILL → ULTRAKILL-Content
2. If you already have ULTRAKILL_Data there, copy/unzip THIS archive and replace matching files.
   (Overlay mode only updates StreamingAssets/aa — keep your existing bundles if this zip has fewer than on phone.)
3. Final path must be: ULTRAKILL-Content/ULTRAKILL_Data/StreamingAssets/aa/catalog.json (iOS paths)
4. filelist.txt at ULTRAKILL-Content root is used by the app to verify copies; re-copy if log reports missing files.

Built: $(Get-Date -Format o)
"@
Set-Content (Join-Path $StagingRoot "README-IPHONE.txt") -Value $readme -Encoding UTF8

if ($SkipZip) {
    Write-Host "Staging only (no zip): $StagingRoot"
    exit 0
}

New-Item -ItemType Directory -Force -Path (Split-Path $OutputZip) | Out-Null
if (Test-Path $OutputZip) { Remove-Item -Force $OutputZip }
$OutputZip = [IO.Path]::GetFullPath($OutputZip)
$winTar = Join-Path $env:SystemRoot "System32\tar.exe"
Write-Host "Creating $OutputZip ..."
& $winTar -a -cf $OutputZip -C $StagingRoot .
if ($LASTEXITCODE -ne 0) { throw "tar failed exit $LASTEXITCODE" }
$mb = [math]::Round((Get-Item $OutputZip).Length / 1MB, 1)
Write-Host "Done: $OutputZip ($mb MB)"
Write-Host "Also left staging folder: $StagingRoot"
