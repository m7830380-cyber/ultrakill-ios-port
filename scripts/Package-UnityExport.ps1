param(
    [string]$UnityProject = "$PSScriptRoot\..\unity-ultrakill",
    [string]$OutFile = "$PSScriptRoot\..\artifacts\unity-export.tar.gz"
)

$ErrorActionPreference = "Stop"
$UnityProject = (Resolve-Path -LiteralPath $UnityProject).Path
New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null

if (Get-Command tar -ErrorAction SilentlyContinue) {
    tar -czf $OutFile -C (Split-Path $UnityProject -Parent) (Split-Path $UnityProject -Leaf) `
        --exclude="Library" --exclude="Temp" --exclude="Logs" --exclude="obj" --exclude="Build"
} else {
    throw "tar not found; install Git for Windows or use WSL to create unity-export.tar.gz"
}
Write-Host "Created $OutFile"
