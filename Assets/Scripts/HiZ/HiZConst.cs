using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Serialization;

namespace HiZRunTime
{
    public static class HiZConst
    {
        public static int _ClusterBuffer = Shader.PropertyToID("_ClusterBuffer");
        public static int _ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");
        public static int _ResultBuffer = Shader.PropertyToID("_ResultBuffer");
        public static int _InstanceBuffer = Shader.PropertyToID("_InstanceBuffer");
        public static int _HizDepthTex = Shader.PropertyToID("_HizDepthTex");
        public static int _MaxCount = Shader.PropertyToID("_MaxCount");
        
        public static int _Planes = Shader.PropertyToID("_Planes");
        public static int _FrustumMinPoint = Shader.PropertyToID("_FrustumMinPoint");
        public static int _FrustumMaxPoint = Shader.PropertyToID("_FrustumMaxPoint");
        public static int _LastVp = Shader.PropertyToID("_LastVp");
        public static int _CameraPos = Shader.PropertyToID("_CameraPos");
        public static int _ClusterOffset = Shader.PropertyToID("_ClusterOffset");
        public static int _HizScreenRes = Shader.PropertyToID("_HizScreenRes");
    }
}

