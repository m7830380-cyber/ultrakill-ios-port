param(
    [string]$RetailData = $env:ULTRAKILL_DATA,
    [string]$Dest = "$PSScriptRoot\..\unity-ultrakill\Assets\Scenes\MainBoot.unity"
)

$ErrorActionPreference = "Stop"
if (-not $RetailData) {
    $RetailData = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data"
}
$level0 = Join-Path $RetailData "level0"
if (-not (Test-Path $level0)) {
    throw "Retail boot scene not found: $level0"
}

New-Item -ItemType Directory -Force -Path (Split-Path $Dest) | Out-Null
Copy-Item $level0 $Dest -Force
Write-Host "Updated MainBoot.unity from retail level0 ($((Get-Item $Dest).Length) bytes)."
