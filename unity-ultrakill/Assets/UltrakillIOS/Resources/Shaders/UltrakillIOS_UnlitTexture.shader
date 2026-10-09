Shader "UltrakillIOS/UnlitTexture"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Colorize ("Colorize", Color) = (1,1,1,1)
		_BumpMap ("Normal", 2D) = "bump" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 100
		Cull Off
		ZWrite On

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed4 _Colorize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// No vertex colors (retail meshes bake lighting as black verts).
				// No real lights on remapped mats — lift dark tunnel textures so
				// shafts aren't pure black while outdoors stay readable.
				fixed4 tex = tex2D(_MainTex, i.uv);
				fixed4 tint = _Color * _Colorize;
				if (tint.r + tint.g + tint.b < 0.15)
				{
					tint = fixed4(1, 1, 1, 1);
				}

				fixed3 rgb = tex.rgb * tint.rgb;
				fixed lum = max(rgb.r, max(rgb.g, rgb.b));
				// Strong lift for dark texels (Tutorial shaft walls).
				fixed lift = saturate(1.0 - lum * 1.8);
				rgb = saturate(rgb * (1.0 + 2.5 * lift) + 0.18 * lift);
				return fixed4(rgb, 1);
			}
			ENDCG
		}
	}
	FallBack "Unlit/Color"
}
