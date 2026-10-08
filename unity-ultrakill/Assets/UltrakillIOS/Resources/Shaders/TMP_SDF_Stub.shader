Shader "TextMeshPro/Distance Field" {
	Properties {
		_FaceColor ("Face Color", Color) = (1,1,1,1)
		_FaceDilate ("Face Dilate", Range(-1,1)) = 0
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_OutlineWidth ("Outline Thickness", Range(0,1)) = 0
		_MainTex ("Font Atlas", 2D) = "white" {}
		_GradientScale ("Gradient Scale", Float) = 5
		_Sharpness ("Sharpness", Range(-1,1)) = 0
		_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
		_MaskSoftnessX ("Mask SoftnessX", Float) = 0
		_MaskSoftnessY ("Mask SoftnessY", Float) = 0
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_CullMode ("Cull Mode", Float) = 0
		_ColorMask ("Color Mask", Float) = 15
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Cull Off
		ZWrite Off
		Blend One OneMinusSrcAlpha
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
			struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
			sampler2D _MainTex; fixed4 _FaceColor; float _FaceDilate; float _GradientScale; float _Sharpness;
			v2f vert(appdata v) {
				v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _FaceColor; return o;
			}
			fixed4 frag(v2f i) : SV_Target {
				half sd = tex2D(_MainTex, i.uv).a;
				half alpha = saturate((sd - (0.5 - _FaceDilate * 0.5)) * (_GradientScale * (1 + _Sharpness)) + 0.5);
				fixed4 c = i.color; c.a *= alpha; c.rgb *= c.a; return c;
			}
			ENDCG
		}
	}
	FallBack "UI/Default"
}
