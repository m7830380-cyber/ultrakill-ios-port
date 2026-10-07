param(
    [string]$RetailPath = $env:ULTRAKILL_RETAIL,
    # Contents of this folder are deleted before export; never point it at unity-ultrakill.
    [string]$ExportPath = "C:\Users\v0id\ultrakill-export",
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

function Set-ExportSettings {
    # Scripts stay as the retail DLL so prefab/scene MonoScript references match the Assembly-CSharp shipped in the IPA.
    # Unchecked HTML checkboxes are omitted, so IgnoreStreamingAssets stays false and the Addressables bundles are exported.
    $settings = @{
        ScriptExportMode           = "DllExportWithoutRenaming"
        ScriptLanguageVersion      = "CSharp10_0"
        ScriptContentLevel         = "Level2"
        ShaderExportMode           = "Decompile"
        BundledAssetsExportMode    = "GroupByBundleName"
        ImageExportFormat          = "Png"
        AudioExportFormat          = "Default"
        TextExportMode             = "Parse"
        SpriteExportMode           = "Texture2D"
        LightmapTextureExportFormat = "Image"
        EnableStaticMeshSeparation = "true"
        DefaultVersion             = "2022.3.29f1"
    }
    Invoke-WebRequest -Uri "$base/Settings/Update" -Method Post -Body $settings -UseBasicParsing -TimeoutSec 60 | Out-Null
    Write-Host "AssetRipper settings applied: scripts=DLL, shaders=Decompile, bundles=GroupByBundleName"
}

$proc = Start-Process -FilePath $AssetRipperExe -WorkingDirectory $arDir -ArgumentList @("--port", $Port, "--launch-browser", "false") -PassThru -WindowStyle Hidden
try {
    Wait-ForAssetRipper
    Set-ExportSettings
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

    $drive = New-Object System.IO.DriveInfo ([System.IO.Path]::GetPathRoot($ExportPath))
    while ($exportJob.State -eq "Running") {
        $freeGb = [math]::Round($drive.AvailableFreeSpace / 1GB, 1)
        Write-Host "$(Get-Date -Format HH:mm:ss) Exporting Unity project... free on $($drive.Name): $freeGb GB"
        if ($freeGb -lt 3) {
            Stop-Job $exportJob
            throw "Aborted export: less than 3 GB free on $($drive.Name)"
        }
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
