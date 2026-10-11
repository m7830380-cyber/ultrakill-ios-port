param(
    [string]$ExportProject = "C:\Users\v0id\ultrakill-export\ExportedProject",
    [string]$OutFile = "$PSScriptRoot\..\artifacts\exported-project.tar.gz"
)

$ErrorActionPreference = "Stop"
$ExportProject = (Resolve-Path -LiteralPath $ExportProject).Path
New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null

if (-not (Get-Command tar -ErrorAction SilentlyContinue)) {
    throw "tar not found; install Git for Windows or use WSL"
}

$parent = Split-Path $ExportProject -Parent
$name = Split-Path $ExportProject -Leaf
tar -czf $OutFile -C $parent $name `
    --exclude="$name/Library" --exclude="$name/Temp" --exclude="$name/Logs" --exclude="$name/obj"
Write-Host "Created $OutFile ($([math]::Round((Get-Item $OutFile).Length / 1GB, 2)) GB)"
Write-Host "Upload to GitHub release tag unity-export as exported-project.tar.gz for CI shader builds."
