param(
    [string]$GameManaged = $env:ULTRAKILL_MANAGED,
    [string]$OutputDir = "$PSScriptRoot\..\artifacts\patched",
    [switch]$MergeIntoAssemblyCSharp
)

$ErrorActionPreference = "Stop"
if (-not $GameManaged) {
    $GameManaged = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\Managed"
}

if (-not (Test-Path (Join-Path $GameManaged "UnityEngine.CoreModule.dll"))) {
    throw "Unity Managed folder not found at $GameManaged. Set ULTRAKILL_MANAGED."
}

$root = Split-Path $PSScriptRoot -Parent
$patchProj = Join-Path $root "patches\MobileTouch\MobileTouch.csproj"
$gameData = Split-Path $GameManaged -Parent

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

dotnet build $patchProj -c Release -p:GameManaged="$GameManaged"

$patchDll = Join-Path $root "patches\MobileTouch\bin\Release\netstandard2.1\UltrakillMobileTouch.dll"
Copy-Item $patchDll (Join-Path $OutputDir "UltrakillMobileTouch.dll") -Force

$scriptingPath = Join-Path $gameData "ScriptingAssemblies.json"
if (Test-Path $scriptingPath) {
    $json = Get-Content $scriptingPath -Raw | ConvertFrom-Json
    if ($json.names -notcontains "UltrakillMobileTouch.dll") {
        $json.names += "UltrakillMobileTouch.dll"
        $json.types += 16
    }
    $json | ConvertTo-Json -Compress | Set-Content (Join-Path $OutputDir "ScriptingAssemblies.json") -Encoding UTF8
}

if ($MergeIntoAssemblyCSharp) {
    $ilRepack = & (Join-Path $PSScriptRoot "ensure-ilrepack.ps1") -ToolsDir (Join-Path $root ".tools")
    $gameDll = Join-Path $GameManaged "Assembly-CSharp.dll"
    $outDll = Join-Path $OutputDir "Assembly-CSharp.dll"
    & $ilRepack /out:$outDll $gameDll $patchDll
    Write-Host "Merged Assembly-CSharp.dll -> $outDll"
} else {
    Write-Host "Sidecar patch: copy UltrakillMobileTouch.dll + ScriptingAssemblies.json into ULTRAKILL_Data (see docs/dnspy.md)."
}

Write-Host "Artifacts in $OutputDir"
