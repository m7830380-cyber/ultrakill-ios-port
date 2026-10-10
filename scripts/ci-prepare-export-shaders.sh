#!/usr/bin/env bash
# Patch AssetRipper export before Unity batch shader bundle build (shared by CI and ci-build-ios-shaders.sh).
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
EXPORT_PROJECT="${EXPORT_PROJECT:?EXPORT_PROJECT required}"
CATALOG_MAP="${CATALOG_MAP:-$REPO_ROOT/ci/catalog-map.json}"

python3 "$REPO_ROOT/scripts/fix_export_shaders.py" "$EXPORT_PROJECT" "$CATALOG_MAP"

map_py=$(python3 - "$CATALOG_MAP" <<'PY'
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
b = next(x for x in data["bundles"] if x["file"] == "assets_assets_assets/shaders.bundle")
print(b["options"]["m_BundleName"] + "_bundle")
PY
)
SHADER_FOLDER="$EXPORT_PROJECT/Assets/Asset_Bundles/$map_py"
EXTRA_STUB='Shader "{0}" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_FaceColor ("Face Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
			struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
			sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; fixed4 _FaceColor;
			v2f vert(appdata v) {
				v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color * _Color * _FaceColor; return o;
			}
			fixed4 frag(v2f i) : SV_Target { return tex2D(_MainTex, i.uv) * i.color; }
			ENDCG
		}
	}
	FallBack "UI/Default"
}'
for name in \
  "UI/Default" "UI/Default Font" \
  "TextMeshPro/Distance Field" "TextMeshPro/Mobile/Distance Field" \
  "TextMeshPro/Distance Field Overlay" "TextMeshPro/Mobile/Distance Field Overlay" \
  "TextMeshPro/Bitmap" "TextMeshPro/Mobile/Bitmap" \
  "GUI/Text Shader" "Sprites/Default"
do
  safe=$(echo "$name" | tr '/ ' '__' | tr -cd '[:alnum:]_-')
  printf "$EXTRA_STUB" "$name" > "$SHADER_FOLDER/extra_${safe}.shader"
done

mkdir -p "$EXPORT_PROJECT/Assets/IosBundleTools/Editor"
cp -f "$REPO_ROOT/export-tools/Editor/"* "$EXPORT_PROJECT/Assets/IosBundleTools/Editor/"

if [[ -d "$EXPORT_PROJECT/Assets/Asset_Bundles" ]]; then
  while IFS= read -r -d '' dir; do
    base=$(basename "$dir")
    parent=$(dirname "$dir")
    new="${base%.bundle}_bundle"
    if [[ "$base" != "$new" && ! -d "$parent/$new" ]]; then
      mv "$dir" "$parent/$new"
      [[ -f "$dir.meta" ]] && mv "$dir.meta" "$parent/$new.meta" || true
    fi
  done < <(find "$EXPORT_PROJECT/Assets/Asset_Bundles" -maxdepth 1 -type d -name '*.bundle' -print0 2>/dev/null || true)
fi

MANIFEST="$EXPORT_PROJECT/Packages/manifest.json"
if [[ -f "$MANIFEST" ]]; then
  python3 - "$MANIFEST" <<'PY'
import json, sys
p = sys.argv[1]
with open(p, encoding="utf-8") as f:
    m = json.load(f)
m.setdefault("dependencies", {})["com.unity.scriptablebuildpipeline"] = "1.21.21"
with open(p, "w", encoding="utf-8") as f:
    json.dump(m, f, indent=2)
    f.write("\n")
PY
fi

echo "Export prepared for iOS shader build at $EXPORT_PROJECT"
