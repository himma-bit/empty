Shader "SliceGenerate"
{
	Properties
	{
		_facadeColor("FacadeColor", Color) = (1,1,1,1)
		_backColor("BackColor", Color) = (1,1,1,1)
		_height("Height", Range(-1,1)) = 0.0
	}
	SubShader
	{
		Tags
		{
			"DisableBatching" = "True"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}

		Pass
		{
			Tags {"RenderType" = "Opaque" "Queue" = "Geometry"}

			Zwrite On
			Cull Off 
			AlphaToMask On //改善AlphaTest抗锯齿

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			#pragma enable_d3d11_debug_symbols

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			float _height;
			float4 _backColor, _facadeColor;
			float4 _SliceObjectPos;

			v2f vert(appdata v)
			{
				v2f o = (v2f)0;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float3 wPos = mul(unity_ObjectToWorld, v.vertex.xyz);
				//_SliceObjectPos是模型的世界空间位置,我们要控制切片范围在box内
				o.worldPos = wPos + _SliceObjectPos.xyz;
				return o;
			}

			float4 frag(v2f i, fixed facing : VFACE) : SV_Target
			{
				float4 heightResult = step(i.worldPos.y + _height,0);

				float4 facade = heightResult * _facadeColor;
				float4 back = heightResult * _backColor;

				float4 finalColor = 0;

				if (facing > 0){
					finalColor = facade;
				}
				else{
					finalColor = back;
				}

				clip(finalColor.a - 0.5);
				return finalColor;
			}
			
			ENDCG
		}
	}
}