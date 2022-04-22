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
    public class BoundsDebugMonoBehaviour : MonoBehaviour
    {
        private Color cubeColor = new Color(0.0f, 1.0f, 0.0f, 0.2f);
        public void OnDrawGizmos()
        {
            var mrs = GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in mrs)
            {
                Gizmos.DrawCube(mr.bounds.center, mr.bounds.size);
            }
        }
    }
}

