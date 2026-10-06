param(
    [string]$Managed = $env:ULTRAKILL_MANAGED,
    [string]$OutputDir = "$PSScriptRoot\..\port-sources\Assembly-CSharp"
)

$ErrorActionPreference = "Stop"
if (-not $Managed) {
    $Managed = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\Managed"
}
$dll = Join-Path $Managed "Assembly-CSharp.dll"
if (-not (Test-Path $dll)) {
    throw "Assembly-CSharp.dll not found at $dll"
}

$ilspy = "$env:USERPROFILE\.dotnet\tools\ilspycmd.exe"
if (-not (Test-Path $ilspy)) {
    throw "ilspycmd not found. Install: dotnet tool install -g ilspycmd"
}

if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "Decompiling $dll -> $OutputDir (same as dnSpy export)..."
& $ilspy $dll -p -o $OutputDir
Write-Host "Done. Point ULTRAKILL_RIP or Setup-UnityPortFromDecompile at parent of Assembly-CSharp folder."
