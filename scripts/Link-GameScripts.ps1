param(
    [string]$UnityProject = "$PSScriptRoot\..\unity-ultrakill",
    [string]$Sources = "$PSScriptRoot\..\game-sources\Assembly-CSharp"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $Sources)) {
    throw "game-sources missing at $Sources — run scripts/Sync-GameSources.ps1 or Decompile-GameScripts.ps1"
}

$link = Join-Path $UnityProject "Assets\Game"
if (Test-Path $link) {
    cmd /c "rmdir `"$link`" 2>nul"
    Remove-Item -Force $link -ErrorAction SilentlyContinue
}

Write-Host "Junction: $link -> $Sources"
cmd /c "mklink /J `"$link`" `"$Sources`""

Write-Host "Open $UnityProject in Unity to edit decompiled game scripts locally."
Write-Host "CI still builds retail Assembly-CSharp.dll; remove Assets/Game before compiling sources in Unity."
