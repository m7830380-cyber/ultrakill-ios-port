Shader "TextMeshPro/Mobile/Distance Field" {
	Properties {
		_FaceColor ("Face Color", Color) = (1,1,1,1)
		_FaceDilate ("Face Dilate", Range(-1,1)) = 0
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_OutlineWidth ("Outline Thickness", Range(0,1)) = 0
		_OutlineSoftness ("Outline Softness", Range(0,1)) = 0
		_WeightNormal ("Weight Normal", Float) = 0
		_WeightBold ("Weight Bold", Float) = 0.5
		_ShaderFlags ("Flags", Float) = 0
		_ScaleRatioA ("Scale Ratio A", Float) = 1
		_ScaleRatioB ("Scale Ratio B", Float) = 1
		_ScaleRatioC ("Scale Ratio C", Float) = 1
		_MainTex ("Font Atlas", 2D) = "white" {}
		_TextureWidth ("Texture Width", Float) = 512
		_TextureHeight ("Texture Height", Float) = 512
		_GradientScale ("Gradient Scale", Float) = 5
		_ScaleX ("Scale X", Float) = 1
		_ScaleY ("Scale Y", Float) = 1
		_PerspectiveFilter ("Perspective Filter", Range(0,1)) = 0.875
		_Sharpness ("Sharpness", Range(-1,1)) = 0
		_VertexOffsetX ("Vertex OffsetX", Float) = 0
		_VertexOffsetY ("Vertex OffsetY", Float) = 0
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
		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		Cull [_CullMode]
		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		ZTest [unity_GUIZTestMode]
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]
		Pass {
			CGPROGRAM
			#pragma vertex VertShader
			#pragma fragment PixShader
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			struct vertex_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord0 : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};
			struct pixel_t {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord0 : TEXCOORD0;
				float4 mask : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _FaceColor;
			float _FaceDilate;
			float _OutlineWidth;
			float _GradientScale;
			float _Sharpness;
			float4 _ClipRect;
			float _MaskSoftnessX;
			float _MaskSoftnessY;
			float _VertexOffsetX;
			float _VertexOffsetY;

			pixel_t VertShader (vertex_t v) {
				pixel_t o;
				float4 vert = v.vertex;
				vert.x += _VertexOffsetX;
				vert.y += _VertexOffsetY;
				o.vertex = UnityObjectToClipPos(vert);
				o.color = v.color * _FaceColor;
				o.texcoord0 = v.texcoord0;
				float2 pixelSize = o.vertex.w;
				pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
				float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
				o.mask = float4(vert.xy * 2 - clampedRect.xy - clampedRect.zw,
					0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
				return o;
			}

			fixed4 PixShader (pixel_t i) : SV_Target {
				half sd = tex2D(_MainTex, i.texcoord0).a;
				half scale = 1.0 + _Sharpness;
				half bias = 0.5 - _FaceDilate * 0.5;
				half alpha = saturate((sd - bias) * (_GradientScale * scale) + 0.5);
				fixed4 c = i.color;
				c.a = c.a * alpha;
				c.rgb *= c.a;
				return c;
			}
			ENDCG
		}
	}
	FallBack "UI/Default"
}
