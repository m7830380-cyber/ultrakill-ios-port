param(
    [string]$RipRoot = $env:ULTRAKILL_RIP,
    [string]$Dest = "$PSScriptRoot\..\game-sources\Assembly-CSharp"
)

$ErrorActionPreference = "Stop"
if (-not $RipRoot) {
    $RipRoot = "C:\Users\v0id\Downloads\cockadoodledo\fdadsfsadfff"
}
$src = Join-Path $RipRoot "Scripts\Assembly-CSharp"
if (-not (Test-Path $src)) {
    throw "Rip scripts not found at $src"
}

if (Test-Path $Dest) {
    Remove-Item -Recurse -Force $Dest
}
New-Item -ItemType Directory -Force -Path $Dest | Out-Null

Write-Host "Copying decompiled game scripts to $Dest (for git push / GitHub CI)..."
robocopy $src $Dest /E /XF *.csproj /XD Properties /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed: $LASTEXITCODE"
}
Write-Host "Done. Commit game-sources/ and push so Actions can compile the port."
