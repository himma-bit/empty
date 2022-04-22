using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace HiZRunTime
{
    [ExecuteInEditMode]
    public class HiZDataMonoBehaviour : MonoBehaviour
    {
        public HiZData hizData;
        

        public bool bDrawBounds = false;
        public bool bDrawCameraFrustumBounds = false;

        public void OnEnable()
        {
            HiZMgr.Instance.hizData = hizData;
        }

        public void OnDisable()
        {
            HiZMgr.Instance.hizData = null;
        }

        private Color cubeColor = new Color(1.0f, 0.0f, 0.0f, 0.2f);
        public void OnDrawGizmos()
        {
            if (bDrawBounds)
            {
                foreach (var draw in hizData.m_allDraws)
                {
                    foreach (var cluster in draw.m_clusters)
                    {
                        Gizmos.color = cubeColor;
                        Gizmos.DrawCube(cluster.m_pos, cluster.m_extent*2);
                    }
                }
            }
            
            if (bDrawCameraFrustumBounds)
            {
                Gizmos.color = Color.green;
                var center = (HiZMgr.Instance.m_maxFrustumPlanes + HiZMgr.Instance.m_minFrustumPlanes);
                center.x /= 2.0f;
                center.y /= 2.0f;
                center.z /= 2.0f;
                var size = HiZMgr.Instance.m_maxFrustumPlanes - HiZMgr.Instance.m_minFrustumPlanes;
                size.x = Mathf.Abs(size.x);
                size.y = Mathf.Abs(size.y);
                size.z = Mathf.Abs(size.z);
                Gizmos.DrawCube(center, size);
            }
            
        }
    }
}

