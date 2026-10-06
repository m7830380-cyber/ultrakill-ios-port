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

$bcl = Join-Path $Root "System.Collections.Generic"
if (Test-Path $bcl) {
    Remove-Item -Recurse -Force $bcl
}

$portalRender = Join-Path $Root "ULTRAKILL.Portal\PortalRenderV2.cs"
if (Test-Path $portalRender) {
    $text = Get-Content $portalRender -Raw
    $text = $text -replace '\bprivate struct\b', 'public struct'
    $text = $text -replace '\bprivate readonly struct\b', 'public readonly struct'
    Set-Content $portalRender $text -Encoding UTF8 -NoNewline
}

Write-Host "Cleaned ilspy artifacts under $Root"
