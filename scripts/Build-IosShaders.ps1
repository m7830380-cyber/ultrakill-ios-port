param(
    [string]$ExportProject = "C:\Users\v0id\ultrakill-export\ExportedProject",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe",
    [string]$RetailAa = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\StreamingAssets\aa",
    [string]$OutRoot = "$PSScriptRoot\..\artifacts\ios-content"
)

# Fast path: rewrite broken AssetRipper shaders to Metal stubs and rebuild only shaders.bundle (+ builtin shader bundle).
$ErrorActionPreference = "Stop"
$repo = Resolve-Path "$PSScriptRoot\.."
$OutRoot = [System.IO.Path]::GetFullPath($OutRoot)
$aaOut = Join-Path $OutRoot "ULTRAKILL_Data\StreamingAssets\aa"
$bundleOut = Join-Path $aaOut "iOS"
$mapPath = Join-Path $repo "artifacts\catalog-map.json"
$shaderOut = Join-Path $env:TEMP "ultrakill-ios-shaders-out"

python (Join-Path $repo "scripts\decode_catalog.py") (Join-Path $RetailAa "catalog.json") $mapPath
if ($LASTEXITCODE -ne 0) { throw "catalog decode failed" }

& (Join-Path $repo "scripts\Fix-ExportShaders.ps1") -ExportProject $ExportProject -CatalogMap $mapPath

# Extra stubs that TMP / UI often Shader.Find by name (not always present in the rip folder).
$map = Get-Content $mapPath -Raw | ConvertFrom-Json
$folderName = ($map.bundles | Where-Object { $_.file -eq "assets_assets_assets/shaders.bundle" }).options.m_BundleName + "_bundle"
$shaderFolder = Join-Path $ExportProject "Assets\Asset_Bundles\$folderName"
$extra = @(
    "UI/Default",
    "UI/Default Font",
    "TextMeshPro/Distance Field",
    "TextMeshPro/Mobile/Distance Field",
    "TextMeshPro/Distance Field Overlay",
    "TextMeshPro/Mobile/Distance Field Overlay",
    "TextMeshPro/Bitmap",
    "TextMeshPro/Mobile/Bitmap",
    "GUI/Text Shader",
    "Sprites/Default"
)
$stub = @'
Shader "{0}" {{
	Properties {{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {{}}
		_Color ("Tint", Color) = (1,1,1,1)
		_FaceColor ("Face Color", Color) = (1,1,1,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_FaceDilate ("Face Dilate", Range(-1,1)) = 0
		_OutlineWidth ("Outline Thickness", Range(0,1)) = 0
	}}
	SubShader {{
		Tags {{ "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }}
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			struct appdata {{ float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; }};
			struct v2f {{ float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; }};
			sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; fixed4 _FaceColor;
			v2f vert(appdata v) {{
				v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color * _Color * _FaceColor; return o;
			}}
			fixed4 frag(v2f i) : SV_Target {{
				fixed4 c = tex2D(_MainTex, i.uv);
				c.a = saturate(c.a * 1.5);
				return c * i.color;
			}}
			ENDCG
		}}
	}}
	FallBack "UI/Default"
}}
'@
foreach ($name in $extra) {
    $safe = ($name -replace '[\\/]', '_') -replace '[^\w\-]', '_'
    $path = Join-Path $shaderFolder ("extra_" + $safe + ".shader")
    [IO.File]::WriteAllText($path, [string]::Format($stub, $name))
}
Write-Host "Wrote $($extra.Count) extra UI/TMP stub shaders"

$tools = Join-Path $ExportProject "Assets\IosBundleTools\Editor"
New-Item -ItemType Directory -Force -Path $tools | Out-Null
Copy-Item (Join-Path $repo "export-tools\Editor\*") $tools -Force

# Ensure SBP package present (idempotent).
$manifestPath = Join-Path $ExportProject "Packages\manifest.json"
if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $manifest.dependencies | Add-Member -NotePropertyName "com.unity.scriptablebuildpipeline" -NotePropertyValue "1.21.21" -Force
    $manifest | ConvertTo-Json -Depth 5 | Set-Content $manifestPath -Encoding UTF8
}

if (Test-Path $shaderOut) { Remove-Item -Recurse -Force $shaderOut }
New-Item -ItemType Directory -Force -Path $shaderOut | Out-Null

$log = Join-Path $repo "artifacts\ios-shaders.log"
Write-Host "Building shaders.bundle only (Unity batch)..."
& $Unity -batchmode -nographics -quit -buildTarget iOS -projectPath $ExportProject -logFile $log `
    -executeMethod IosRetailBundleBuild.Build `
    -catalogMap $mapPath -outDir $shaderOut `
    -onlyBundle "assets_assets_assets/shaders.bundle"
if ($LASTEXITCODE -ne 0) { throw "Unity shader bundle build failed (exit $LASTEXITCODE); see $log" }

$built = Get-ChildItem $shaderOut -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq "shaders.bundle" } |
    Sort-Object Length -Descending |
    Select-Object -First 1
if (-not $built) {
    Write-Host "Output tree:"; Get-ChildItem $shaderOut -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object FullName
    throw "shaders.bundle was not produced; see $log"
}
Write-Host "Built shaders.bundle at $($built.FullName) ($([math]::Round($built.Length/1MB, 2)) MB)"

$destDir = Join-Path $bundleOut "assets_assets_assets"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item $built.FullName (Join-Path $destDir "shaders.bundle") -Force

# Builtin shader bundle from the same build, if present.
$builtin = Get-ChildItem $shaderOut -Recurse -File | Where-Object { $_.Name -eq "shader_unitybuiltinshaders.bundle" } | Select-Object -First 1
if ($builtin) {
    Copy-Item $builtin.FullName (Join-Path $bundleOut "shader_unitybuiltinshaders.bundle") -Force
}

$dest = Join-Path $destDir "shaders.bundle"
Write-Host "Updated $($dest) ($([math]::Round((Get-Item $dest).Length/1MB, 2)) MB)"
Write-Host "Copy the whole aa folder (or at least iOS/assets_assets_assets/shaders.bundle + shader_unitybuiltinshaders.bundle) to the phone."
