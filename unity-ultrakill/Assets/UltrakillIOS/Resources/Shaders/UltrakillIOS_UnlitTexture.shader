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
				// Do NOT multiply mesh vertex colors — retail meshes often store
				// baked lighting as black verts, which made the whole Tutorial black.
				fixed4 tex = tex2D(_MainTex, i.uv);
				fixed4 tint = _Color * _Colorize;
				// Guard near-black tints / missing textures.
				if (tint.r + tint.g + tint.b < 0.15)
				{
					tint = fixed4(1, 1, 1, 1);
				}
				if (tex.r + tex.g + tex.b < 0.02 && tex.a > 0.5)
				{
					tex = fixed4(0.75, 0.75, 0.8, 1);
				}
				return tex * tint;
			}
			ENDCG
		}
	}
	FallBack "Unlit/Color"
}
