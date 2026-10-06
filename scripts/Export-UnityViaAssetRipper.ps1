param(
    [string]$RetailPath = $env:ULTRAKILL_RETAIL,
    [string]$ExportPath = "$PSScriptRoot\..\unity-ultrakill",
    [int]$Port = 53335,
    [string]$AssetRipperExe = "$PSScriptRoot\..\.tools\ar\AssetRipper.GUI.Free.exe"
)

$ErrorActionPreference = "Stop"
if (-not $RetailPath) {
    $RetailPath = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL"
}
if (-not (Test-Path $AssetRipperExe)) {
    throw "AssetRipper not found at $AssetRipperExe (extract AssetRipper_win_x64.zip to .tools/ar)"
}

$ExportPath = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $ExportPath)).Path
$logDir = Join-Path $PSScriptRoot "..\artifacts\assetripper"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$proc = Start-Process -FilePath $AssetRipperExe -ArgumentList @("--port", $Port, "--headless") -PassThru -WindowStyle Hidden
$base = "http://127.0.0.1:$Port"

try {
    $ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            Invoke-WebRequest -Uri $base -UseBasicParsing -TimeoutSec 3 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    if (-not $ready) { throw "AssetRipper web UI did not start on port $Port" }

    Write-Host "Loading retail folder: $RetailPath"
    Invoke-WebRequest -Uri "$base/LoadFolder" -Method Post -Body @{ Path = $RetailPath } -TimeoutSec 7200 | Out-Null

    Write-Host "Exporting Unity project to: $ExportPath"
    if (Test-Path $ExportPath) {
        Remove-Item -Recurse -Force $ExportPath
    }
    Invoke-WebRequest -Uri "$base/Export/UnityProject" -Method Post -Body @{ Path = $ExportPath; CreateSubfolder = "false" } -TimeoutSec 14400 | Out-Null

    if (-not (Test-Path (Join-Path $ExportPath "ProjectSettings\ProjectVersion.txt"))) {
        throw "Export failed: ProjectSettings missing under $ExportPath"
    }
    Write-Host "Export complete."
} finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
