Shader "SliceBake"
{
	Properties
	{
		
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
			Tags {"RenderType" = "Opaque" "Queue" = "Opaque"}

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
			};

			float _ClipHeight;

			v2f vert(appdata v)
			{
				v2f o = (v2f)0;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float4 frag(v2f i, fixed facing : VFACE) : SV_Target
			{
				fixed linearZ = i.vertex.z/i.vertex.w;

				#if UNITY_REVERSED_Z
					half height = 1-_ClipHeight;
					clip(height - linearZ);
				#else
					clip(linearZ - _ClipHeight);
				#endif

				if (facing < 0)
					return float4(1,0,0,1);
				else
					return float4(0,0,0,1);
			}
			
			ENDCG
		}
	}
}