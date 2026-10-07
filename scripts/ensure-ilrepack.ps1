param(
    [string]$ToolsDir = "$PSScriptRoot\..\.tools"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null

$ilRepack = Join-Path $ToolsDir "ILRepack.exe"
if (Test-Path $ilRepack) {
    return $ilRepack
}

$src = Join-Path $ToolsDir "il-repack-src"
if (-not (Test-Path (Join-Path $src ".git"))) {
    git clone --depth 1 --branch 2.0.18 https://github.com/gluck/il-repack.git $src
}

$out = Join-Path $ToolsDir "ilrepack-out"
dotnet publish (Join-Path $src "ILRepack\ILRepack.csproj") -c Release -o $out
$built = Join-Path $out "ILRepack.exe"
if (-not (Test-Path $built)) {
    throw "ILRepack publish failed; expected $built"
}

Copy-Item $built $ilRepack -Force
return $ilRepack
