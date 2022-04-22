Shader "Hidden/GenerateDepthRT"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True" }

        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            Name "LightweightForward"
            Tags { "LightMode" = "LightweightForward" }
            
            HLSLPROGRAM

            #pragma enable_d3d11_debug_symbols
            
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D_FLOAT(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            float4 _CameraDepthTexture_TexelSize;

            struct Attributes
            {
                float4 positioonOS  : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
                
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positioonOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float frag (Varyings i) : SV_Target
            {
                float2 offset = _CameraDepthTexture_TexelSize.xy * 0.5;
                float x = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture,sampler_CameraDepthTexture, i.uv + offset, 0).x;
                float y = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture,sampler_CameraDepthTexture, i.uv - offset, 0).x;
                float z = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture,sampler_CameraDepthTexture, i.uv + float2(offset.x, -offset.y), 0).x;
                float w = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture,sampler_CameraDepthTexture, i.uv + float2(-offset.x, offset.y), 0).x;
                float4 readDepth = float4(x,y,z,w);
                #if UNITY_REVERSED_Z
                    readDepth.xy = min(readDepth.xy, readDepth.zw);
                    readDepth.x = min(readDepth.x, readDepth.y);
                #else
                    readDepth.xy = max(readDepth.xy, readDepth.zw);
                    readDepth.x = max(readDepth.x, readDepth.y);
                #endif
                return readDepth.x;
            }
            ENDHLSL
        }
    }
}