param(
    [string]$RetailPath = $env:ULTRAKILL_RETAIL,
    [string]$ExportPath = "$PSScriptRoot\..\unity-ultrakill",
    [int]$Port = 53342,
    [string]$AssetRipperExe = "$PSScriptRoot\..\.tools\ar\AssetRipper.GUI.Free.exe"
)

$ErrorActionPreference = "Stop"
if (-not $RetailPath) {
    $RetailPath = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL"
}
if (-not (Test-Path $AssetRipperExe)) {
    throw "AssetRipper not found at $AssetRipperExe"
}

$ExportPath = (New-Item -ItemType Directory -Force -Path $ExportPath).FullName
$arDir = Split-Path -Parent $AssetRipperExe
$base = "http://127.0.0.1:$Port"
$httpTimeout = 6 * 3600

function Wait-ForAssetRipper {
    for ($i = 0; $i -lt 120; $i++) {
        try {
            Invoke-WebRequest -Uri $base -UseBasicParsing -TimeoutSec 3 | Out-Null
            return
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    throw "AssetRipper web UI did not start on port $Port"
}

$proc = Start-Process -FilePath $AssetRipperExe -WorkingDirectory $arDir -ArgumentList @("--port", $Port, "--launch-browser", "false") -PassThru -WindowStyle Hidden
try {
    Wait-ForAssetRipper
    Write-Host "Loading retail folder: $RetailPath"

    $loadJob = Start-Job -ScriptBlock {
        param($Uri, $Path, $Timeout)
        Invoke-WebRequest -Uri $Uri -Method Post -Body @{ Path = $Path } -TimeoutSec $Timeout | Out-Null
    } -ArgumentList "$base/LoadFolder", $RetailPath, $httpTimeout

    while ($loadJob.State -eq "Running") {
        Write-Host "$(Get-Date -Format HH:mm:ss) AssetRipper still processing game files..."
        Start-Sleep -Seconds 45
    }
    Receive-Job $loadJob -ErrorAction Stop | Out-Null
    Remove-Job $loadJob -Force

    if (Test-Path $ExportPath) {
        Get-ChildItem $ExportPath -Force | Remove-Item -Recurse -Force
    }

    Write-Host "Exporting Unity project to: $ExportPath"
    $exportJob = Start-Job -ScriptBlock {
        param($Uri, $Path, $Timeout)
        Invoke-WebRequest -Uri $Uri -Method Post -Body @{ Path = $Path; CreateSubfolder = "false" } -TimeoutSec $Timeout | Out-Null
    } -ArgumentList "$base/Export/UnityProject", $ExportPath, $httpTimeout

    while ($exportJob.State -eq "Running") {
        Write-Host "$(Get-Date -Format HH:mm:ss) Exporting Unity project..."
        Start-Sleep -Seconds 60
    }
    Receive-Job $exportJob -ErrorAction Stop | Out-Null
    Remove-Job $exportJob -Force

    if (-not (Test-Path (Join-Path $ExportPath "ProjectSettings\ProjectVersion.txt"))) {
        throw "Export failed: ProjectSettings missing under $ExportPath"
    }
    Write-Host "Export complete."
} finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
