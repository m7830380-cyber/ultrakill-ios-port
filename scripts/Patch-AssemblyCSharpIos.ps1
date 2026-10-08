param(
    [string]$Dll = "$PSScriptRoot\..\unity-ios\Assets\Plugins\UltrakillManaged\Assembly-CSharp.dll"
)

$ErrorActionPreference = "Stop"
$Dll = Resolve-Path $Dll
$patcher = Join-Path $PSScriptRoot "RetailDllPatcher"
dotnet run --project $patcher -c Release -- $Dll
