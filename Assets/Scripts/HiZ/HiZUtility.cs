using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace HiZRunTime
{
    public struct PerspCam
    {
        public float3 right;
        public float3 up;
        public float3 forward;
        public float3 position;
        public float fov;
        public float nearClipPlane;
        public float farClipPlane;
        public float aspect;
    }
    
    public static class HiZUtility
    {
        public const float ScreenHeightMinPercent = 0.05f;
        public const float CameraFov = Mathf.PI / 4;
        public static float CalculateDisplayDistanceMax(Bounds bounds)
        {
            var radius = bounds.size.magnitude / 2;
            radius = Mathf.Pow(radius, 0.7f); 
            var ratio = ScreenHeightMinPercent * Mathf.Tan(CameraFov / 2);
            return radius / ratio;
        }
        
        public static byte[] ToRawBytes<T>(this List<T> arr) where T : struct
        {
            var nativeArray = new NativeArray<T>(arr.ToArray(), Allocator.Temp);
            var bytes = nativeArray.ToRawBytes();
            nativeArray.Dispose();
            return bytes;
        }
        
        public static byte[] ToRawBytes<T>(this NativeArray<T> arr) where T : struct
        {
            var slice = new NativeSlice<T>(arr).SliceConvert<byte>();
            var bytes = new byte[slice.Length];
            slice.CopyTo(bytes);
            return bytes;
        }

        public static void CopyFromRawBytes<T>(this NativeArray<T> arr, byte[] bytes) where T : struct
        {
            var byteArr = new NativeArray<byte>(bytes, Allocator.Temp);
            var slice = new NativeSlice<byte>(byteArr).SliceConvert<T>();

            UnityEngine.Debug.Assert(arr.Length == slice.Length);
            slice.CopyTo(arr);
        }


        public static NativeArray<T> FromRawBytes<T>(byte[] bytes, Allocator allocator) where T : struct
        {
            int structSize = UnsafeUtility.SizeOf<T>();

            UnityEngine.Debug.Assert(bytes.Length % structSize == 0);

            int length = bytes.Length / UnsafeUtility.SizeOf<T>();
            var arr = new NativeArray<T>(length, allocator);
            arr.CopyFromRawBytes(bytes);
            return arr;
        }
        
        public static void GetFrustumCorner(ref PerspCam perspCam, Vector3[] corners)
        {
            float fov = Mathf.Tan(Mathf.Deg2Rad * perspCam.fov * 0.5f);
            void GetCorner(float dist, int step, ref PerspCam persp)
            {
                float upLength = dist * (fov);
                float rightLength = upLength * persp.aspect;
                float3 farPoint = persp.position + dist * persp.forward;
                float3 upVec = upLength * persp.up;
                float3 rightVec = rightLength * persp.right;
                corners[step*4+0] = farPoint - upVec - rightVec;
                corners[step*4+1] = farPoint - upVec + rightVec;
                corners[step*4+2] = farPoint + upVec - rightVec;
                corners[step*4+3] = farPoint + upVec + rightVec;
            }
            GetCorner(perspCam.nearClipPlane, 0, ref perspCam);
            GetCorner(perspCam.farClipPlane, 1, ref perspCam);
        }
    }
}

