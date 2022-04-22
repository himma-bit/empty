Shader "SlicePreviewBlur"
{
	Properties
	{
		[NoScaleOffset]_VolumeTex("VolumeTex",3D) = "black"{}
		[HideInInspector]_offset("Offset", float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		
		Pass
		{
			ZWrite Off
			Blend One OneMinusSrcAlpha
		    Cull Off

			CGPROGRAM

			#pragma enable_d3d11_debug_symbols

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

			Texture3D<float4> _VolumeTex;
			SamplerState  sampler_VolumeTex;
			float _offset;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(float4(v.vertex.xyz, 1.0));
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{       
				float3 uvw = float3(i.uv.x, _offset, i.uv.y);
				float4 color = _VolumeTex.SampleLevel(sampler_VolumeTex, uvw, 0);
				return fixed4(color.r, 0, 0, step(0.5, color.r));
			}
			ENDCG
		}
	}
}