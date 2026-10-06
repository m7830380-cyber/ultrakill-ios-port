param(
    [string]$Root = "$PSScriptRoot\..\game-sources\Assembly-CSharp"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $Root)) {
    return
}

$props = Join-Path $Root "Properties"
if (Test-Path $props) {
    Remove-Item -Recurse -Force $props
}

foreach ($name in @(
        "Assembly-CSharp.csproj",
        "UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs",
        "-PrivateImplementationDetails-.cs"
    )) {
    $p = Join-Path $Root $name
    if (Test-Path $p) {
        Remove-Item -Force $p
    }
}

Get-ChildItem $Root -Filter "__JobReflectionRegistrationOutput__*.cs" -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

Write-Host "Cleaned ilspy artifacts under $Root"
