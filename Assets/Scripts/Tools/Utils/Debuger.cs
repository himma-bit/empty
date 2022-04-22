using System;
using HiZRunTime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [ExecuteInEditMode]
    public class Debuger : MonoBehaviour
    {
        public float disToCamera;
        public float[] autoShowDis;

        protected void OnDisable()
        {
            
        }

        protected void OnEnable()
        {
            var mrs = gameObject.GetComponentsInChildren<MeshRenderer>();
            autoShowDis = new float[mrs.Length];
            for (int i = 0; i < autoShowDis.Length; i++)
            {
                autoShowDis[i] = HiZUtility.CalculateDisplayDistanceMax(mrs[i].bounds);
            }
        }

        protected void Update()
        {
            if (SceneView.lastActiveSceneView)
                disToCamera = (SceneView.lastActiveSceneView.camera.transform.position - transform.position).magnitude;
        }
    }

}