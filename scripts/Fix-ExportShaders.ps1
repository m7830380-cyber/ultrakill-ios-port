param(
    [string]$ExportProject = "C:\Users\v0id\ultrakill-export\ExportedProject",
    [string]$CatalogMap = "$PSScriptRoot\..\artifacts\catalog-map.json"
)

# AssetRipper's Decompile mode dumps broken D3D stubs. Replace every .shader with a Metal-friendly
# Unlit pass that keeps the original Shader "Name" so materials / Shader.Find still resolve.
$ErrorActionPreference = "Stop"

$map = Get-Content $CatalogMap -Raw | ConvertFrom-Json
$shaderBundle = $map.bundles | Where-Object { $_.file -eq "assets_assets_assets/shaders.bundle" }
if (-not $shaderBundle) { throw "shaders.bundle not found in catalog map" }

$folderName = $shaderBundle.options.m_BundleName + "_bundle"
$folder = Join-Path $ExportProject "Assets\Asset_Bundles\$folderName"
if (-not (Test-Path $folder)) { throw "Shader export folder missing: $folder" }

# Opaque textured unlit — retail ULTRAKILL world materials are mostly opaque; transparent
# stubs caused white/purple garbage on iOS when paired with a tiny shaders.bundle.
$stubTemplate = @'
Shader "{0}" {{
	Properties {{
		_MainTex ("Texture", 2D) = "white" {{}}
		_Color ("Color", Color) = (1,1,1,1)
		_Colorize ("Colorize", Color) = (1,1,1,1)
		_Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
	}}
	SubShader {{
		Tags {{ "RenderType"="Opaque" "Queue"="Geometry" }}
		Cull Off
		ZWrite On
		Pass {{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			struct appdata {{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			}};
			struct v2f {{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			}};
			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed4 _Colorize;
			v2f vert (appdata v) {{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}}
			fixed4 frag (v2f i) : SV_Target {{
				fixed4 tex = tex2D(_MainTex, i.uv);
				fixed3 tint = _Color.rgb * _Colorize.rgb;
				return fixed4(tex.rgb * tint, tex.a * _Color.a);
			}}
			ENDCG
		}}
	}}
	FallBack "Unlit/Texture"
}}
'@

$renamed = 0
Get-ChildItem $folder -Filter "*.shader" | ForEach-Object {
    $all = [IO.File]::ReadAllText($_.FullName)
    $head = if ($all.Length -gt 500) { $all.Substring(0, 500) } else { $all }
    $m = [regex]::Match($head, 'Shader\s+"([^"]+)"')
    if (-not $m.Success) {
        Write-Warning "No Shader name in $($_.Name); skipping"
        return
    }
    $name = $m.Groups[1].Value
    $body = [string]::Format($stubTemplate, $name)
    [IO.File]::WriteAllText($_.FullName, $body)
    $renamed++
}

Write-Host "Replaced $renamed shaders in $folder with Metal-compatible stubs (names preserved)."
