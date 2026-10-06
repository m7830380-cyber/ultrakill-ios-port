param(
    [string]$UnityProject = "$PSScriptRoot\..\unity-ios",
    [string]$Output = "$PSScriptRoot\..\artifacts\ultrakill-managed.tar.gz"
)

$ErrorActionPreference = "Stop"
$plugins = Join-Path $UnityProject "Assets\Plugins\UltrakillManaged"
if (-not (Test-Path $plugins)) {
    & (Join-Path $PSScriptRoot "Stage-RetailForUnityIos.ps1") -UnityProject $UnityProject
}

$staging = Join-Path $env:TEMP "ultrakill-managed-tar"
if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
New-Item -ItemType Directory -Force -Path (Join-Path $staging "UltrakillManaged") | Out-Null
Copy-Item (Join-Path $plugins "*") (Join-Path $staging "UltrakillManaged") -Recurse -Force

$link = Join-Path $UnityProject "Assets\link.xml"
if (Test-Path $link) {
    Copy-Item $link $staging -Force
}

$Output = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null
if (Test-Path $Output) { Remove-Item -Force $Output }
& "$env:SystemRoot\System32\tar.exe" -czf $Output -C $staging .
Write-Host "Created $Output. Upload to GitHub release tag build-deps as ultrakill-managed.tar.gz for CI."
