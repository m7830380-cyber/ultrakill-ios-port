"""Replace ripped .shader stubs in the AssetRipper export with Metal-friendly unlit passes (keeps Shader names).

Usage: python fix_export_shaders.py <ExportedProject> <catalog-map.json>
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

STUB_TEMPLATE = r'''Shader "{name}" {{
	Properties {{
		_MainTex ("Texture", 2D) = "white" {{}}
		_BlendTex ("Blend", 2D) = "white" {{}}
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
'''

SHADER_NAME_RE = re.compile(r'Shader\s+"([^"]+)"')


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2

    export = Path(sys.argv[1]).resolve()
    map_path = Path(sys.argv[2]).resolve()
    data = json.loads(map_path.read_text(encoding="utf-8"))
    shader_bundle = next(
        (b for b in data["bundles"] if b["file"] == "assets_assets_assets/shaders.bundle"),
        None,
    )
    if not shader_bundle:
        raise SystemExit("shaders.bundle not found in catalog map")

    folder_name = shader_bundle["options"]["m_BundleName"] + "_bundle"
    folder = export / "Assets" / "Asset_Bundles" / folder_name
    if not folder.is_dir():
        raise SystemExit(f"Shader export folder missing: {folder}")

    renamed = 0
    for path in folder.glob("*.shader"):
        head = path.read_text(encoding="utf-8", errors="replace")[:500]
        m = SHADER_NAME_RE.search(head)
        if not m:
            print(f"warning: no Shader name in {path.name}; skipping", file=sys.stderr)
            continue
        body = STUB_TEMPLATE.format(name=m.group(1))
        path.write_text(body, encoding="utf-8")
        renamed += 1

    # Master shaders are often missing from AssetRipper export but required by most materials.
    for fname, shader_name in (
        ("ULTRAKILL-Standard.shader", "ULTRAKILL-Standard"),
        ("ULTRAKILL-Stationary.shader", "ULTRAKILL-Stationary"),
    ):
        path = folder / fname
        if not path.exists():
            path.write_text(STUB_TEMPLATE.format(name=shader_name), encoding="utf-8")
            print(f"Injected missing master stub: {fname}")

    print(f"Rewrote {renamed} shaders in {folder}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
