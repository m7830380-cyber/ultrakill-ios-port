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
		ZTest LEqual

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
				fixed4 tex = tex2D(_MainTex, i.uv);
				fixed3 tint = _Color.rgb * _Colorize.rgb;
				if (dot(tint, 1) < 0.01) tint = fixed3(1, 1, 1);
				return fixed4(tex.rgb * tint, tex.a * _Color.a);
			}
			ENDCG
		}
	}
	FallBack "Unlit/Color"
}
