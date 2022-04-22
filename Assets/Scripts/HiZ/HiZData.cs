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
    [Serializable]
    public struct ClusterData
    {
        public Vector3 m_pos;
        public Vector3 m_extent;
        public float m_displayDistanceMin;
        public float m_displayDistanceMax;
        public uint m_drawIndex;
        public uint m_clusterOffset;
        
        public ClusterData(Bounds bounds, float min, float max, uint drawIndex)
        {
            this.m_pos = bounds.center;
            this.m_extent = bounds.extents;
            this.m_displayDistanceMin = min;
            this.m_displayDistanceMax = max;
            this.m_drawIndex = drawIndex;
            this.m_clusterOffset = 0;
        }
        
        public ClusterData(ClusterData other, uint mClusterOffset)
        {
            this.m_pos = other.m_pos;
            this.m_extent = other.m_extent;
            this.m_displayDistanceMin = other.m_displayDistanceMin;
            this.m_displayDistanceMax = other.m_displayDistanceMax;
            this.m_drawIndex = other.m_drawIndex;
            this.m_clusterOffset = mClusterOffset;
        }
    }
    
    [Serializable]
    public struct InstanceData
    {
        public Matrix4x4 m_mat;
        public Matrix4x4 m_InverseMat;

        public InstanceData(Matrix4x4 mMat, Matrix4x4 mInverseMat)
        {
            this.m_mat = mMat;
            this.m_InverseMat = mInverseMat;
        }
    }
    
    [Serializable]
    public struct DrawParamsKey
    {
        public string m_meshPath;
        public int m_meshIndex;
        public int m_submeshIndex;
        public string m_matPath;
            
        public DrawParamsKey(string mMeshPath, int mMeshIndex, int mSubmeshIndex, string mMatPath)
        {
            this.m_meshPath = mMeshPath;
            this.m_meshIndex = mMeshIndex;
            this.m_submeshIndex = mSubmeshIndex;
            this.m_matPath = mMatPath;
        }
            
        public static bool operator ==(DrawParamsKey c1, DrawParamsKey c2)
        {
            return c1.m_meshPath == c2.m_meshPath && c1.m_meshIndex == c2.m_meshIndex && c1.m_submeshIndex == c2.m_submeshIndex && c1.m_matPath == c2.m_matPath;
        }

        public static bool operator !=(DrawParamsKey c1, DrawParamsKey c2) 
        {
            return c1.m_meshPath != c2.m_meshPath || c1.m_meshIndex != c2.m_meshIndex || c1.m_submeshIndex != c2.m_submeshIndex || c1.m_matPath == c2.m_matPath;
        }
    }
    
    [Serializable]
    public class DrawParams
    {
        public DrawParams(DrawParamsKey key, uint drawIndex)
        {
            this.m_meshPath = key.m_meshPath;
            this.m_meshIndex = key.m_meshIndex;
            this.m_submeshIndex = key.m_submeshIndex;
            this.m_matPath = key.m_matPath;
            this.m_drawIndex = drawIndex;
            
#if UNITY_EDITOR
            this.m_clusters = new List<ClusterData>();
            this.m_instances = new List<InstanceData>();
#endif
        }
        
        #region SerializeField
        [SerializeField]
        public uint m_drawIndex;
        [SerializeField]
        public uint m_clusterOffset;
        [SerializeField]
        public string m_meshPath;
        [SerializeField]
        public int m_meshIndex;
        [SerializeField]
        public int m_submeshIndex;
        [SerializeField]
        public string m_matPath;
        [SerializeField]
        public byte[] m_runtimeInstances;
        [SerializeField] 
        public int m_instanceCount;
        #endregion

        #region NonSerialized | RunTime
        [NonSerialized]
        public Mesh m_mesh;
        [NonSerialized]
        public Material m_mat;
        [NonSerialized] 
        public ComputeBuffer m_instanceBuffer;
        #endregion
        
#if UNITY_EDITOR
        #region EditTime
        // [NonSerialized]
        [SerializeField]
        // just for debug
        public List<ClusterData> m_clusters;
        // [NonSerialized] 
        [SerializeField]
        // just for debug
        public List<InstanceData> m_instances;
        #endregion
#endif
    }

    [Serializable]
    public class HiZData : ScriptableObject
    {
        [SerializeField] 
        public List<DrawParams> m_allDraws;
 
        [SerializeField]
        public byte[] m_clusters;
        [SerializeField]
        public int m_clustersCount;
    }
}

