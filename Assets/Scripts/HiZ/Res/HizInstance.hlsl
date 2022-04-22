#ifndef UNIVERSAL_HIZ_INSTANCE_INCLUDED
#define UNIVERSAL_HIZ_INSTANCE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    struct HizInstanceData
    {
        float4x4 mat;
        float4x4 inverseMat;
    };
    StructuredBuffer<HizInstanceData> _InstanceBuffer;
    StructuredBuffer<uint> _ResultBuffer;
 
    void unity_instancing_procedural_func()
    {
        uint instanceIdex = _ResultBuffer[_ClusterOffset + unity_InstanceID];
        HizInstanceData data = _InstanceBuffer[instanceIdex];
        unity_ObjectToWorld = data.mat;
        unity_WorldToObject = data.inverseMat;
    }
#endif
    
#endif
