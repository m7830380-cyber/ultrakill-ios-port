param(
    [string]$ExportProject = "C:\Users\v0id\ultrakill-export\ExportedProject",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe",
    [string]$RetailAa = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\StreamingAssets\aa",
    [string]$OutRoot = "$PSScriptRoot\..\artifacts\ios-content",
    [string]$OnlyBundle = ""
)

# Prepares the AssetRipper export for an iOS bundle build, runs it, and writes an aa folder (iOS bundles plus the
# retail catalog pointed at them) under $OutRoot/ULTRAKILL_Data/StreamingAssets/aa.
$ErrorActionPreference = "Stop"
$repo = Resolve-Path "$PSScriptRoot\.."
$OutRoot = [System.IO.Path]::GetFullPath($OutRoot)
$aaOut = Join-Path $OutRoot "ULTRAKILL_Data\StreamingAssets\aa"
$bundleOut = Join-Path $aaOut "iOS"
$mapPath = Join-Path $repo "artifacts\catalog-map.json"

python (Join-Path $repo "scripts\decode_catalog.py") (Join-Path $RetailAa "catalog.json") $mapPath
if ($LASTEXITCODE -ne 0) { throw "catalog decode failed" }

$tools = Join-Path $ExportProject "Assets\IosBundleTools\Editor"
New-Item -ItemType Directory -Force -Path $tools | Out-Null
Copy-Item (Join-Path $repo "export-tools\Editor\*") $tools -Force

# Unity imports a "*.bundle" folder as one macOS plugin and ignores the assets inside.
foreach ($dir in Get-ChildItem (Join-Path $ExportProject "Assets\Asset_Bundles") -Directory -Filter "*.bundle") {
    $newName = $dir.Name -replace '\.bundle$', '_bundle'
    Rename-Item $dir.FullName $newName
    if (Test-Path "$($dir.FullName).meta") { Rename-Item "$($dir.FullName).meta" "$newName.meta" }
}

# AssetRipper disables its plugin DLLs for the Editor, which leaves every MonoBehaviour without a script at build time.
foreach ($meta in Get-ChildItem (Join-Path $ExportProject "Assets\Plugins") -Recurse -Filter "*.dll.meta") {
    $text = [System.IO.File]::ReadAllText($meta.FullName)
    $patched = [regex]::Replace($text, '(Editor: Editor\r?\n\s+second:\r?\n\s+enabled: )0', '${1}1')
    if ($patched -ne $text) { [System.IO.File]::WriteAllText($meta.FullName, $patched) }
}

# The export ships SBP's runtime DLL; the editor half comes from the package, which also provides the runtime.
Get-ChildItem (Join-Path $ExportProject "Assets\Plugins") -Recurse -Filter "Unity.ScriptableBuildPipeline.dll*" | Remove-Item -Force
$manifestPath = Join-Path $ExportProject "Packages\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName "com.unity.scriptablebuildpipeline" -NotePropertyValue "1.21.21" -Force
$manifest | ConvertTo-Json -Depth 5 | Set-Content $manifestPath -Encoding UTF8

if (-not $OnlyBundle -and (Test-Path $bundleOut)) {
    Remove-Item -Recurse -Force $bundleOut
}
New-Item -ItemType Directory -Force -Path $bundleOut | Out-Null
$log = Join-Path $repo "artifacts\ios-bundles.log"
$extra = @()
if ($OnlyBundle) {
    $extra += "-onlyBundle"
    $extra += $OnlyBundle
}
& $Unity -batchmode -nographics -quit -buildTarget iOS -projectPath $ExportProject -logFile $log `
    -executeMethod IosRetailBundleBuild.Build -catalogMap $mapPath -outDir $bundleOut @extra | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Unity bundle build failed (exit $LASTEXITCODE); see $log" }

$catalog = Get-Content (Join-Path $RetailAa "catalog.json") -Raw
$catalog = $catalog.Replace('\\StandaloneWindows64\\', '\\iOS\\')
[System.IO.File]::WriteAllText((Join-Path $aaOut "catalog.json"), $catalog)
$settings = (Get-Content (Join-Path $RetailAa "settings.json") -Raw).Replace('"StandaloneWindows64"', '"iOS"')
[System.IO.File]::WriteAllText((Join-Path $aaOut "settings.json"), $settings)
Copy-Item (Join-Path $RetailAa "AddressablesLink") $aaOut -Recurse -Force

$bundles = Get-ChildItem $bundleOut -Recurse -Filter "*.bundle"
Write-Host "iOS bundles: $($bundles.Count), $([math]::Round(($bundles | Measure-Object Length -Sum).Sum / 1MB)) MB in $aaOut"
