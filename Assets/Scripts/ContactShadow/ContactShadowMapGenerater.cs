using System;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEditor;
using UnityEngine.Serialization;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

namespace HiZRunTime
{
    public class ContactShadowMapGenerater : ScriptableRendererFeature
    {
        private ContactShadowMapPass m_ContactShadowMapPass;
        
        public RenderPassEvent m_RenderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        public int m_RenderPassEventOffset = 1;
        public ContactShadows m_ContactShadows;
        
        public override void Create()
        {
            m_ContactShadowMapPass.UpdateContaceShadowParams(m_ContactShadows);
        }

        public void OnEnable()
        {
            m_ContactShadows = new ContactShadows();
            m_ContactShadowMapPass = new ContactShadowMapPass(m_RenderPassEvent+m_RenderPassEventOffset, m_ContactShadows);
            Shader.EnableKeyword("_CONTACT_SHADOW");
        }

        public void OnDisable()
        {
            Shader.DisableKeyword("_CONTACT_SHADOW");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ContactShadowMapPass);
        }
    }

    public class ContactShadowMapPass : ScriptableRenderPass
    {
        private string m_ContactShadowMapProfileTag = "ContactShadowMap";
        private ProfilingSampler m_ContactShadowMapProfile;
        
        private ComputeShader m_ContactShadowComputeShader;
        private int m_DeferredContactShadowKernel;

        private ContactShadows m_ContactShadows;
        private RenderTexture m_ContactShadowMap;

        public static readonly int st_ContactShadowParamsParametersID = Shader.PropertyToID("_ContactShadowParamsParameters");
        public static readonly int st_ContactShadowParamsParameters2ID = Shader.PropertyToID("_ContactShadowParamsParameters2");
        public static readonly int st_ContactShadowParamsParameters3ID = Shader.PropertyToID("_ContactShadowParamsParameters3");
        public static readonly int st_ContactShadowTextureUAVID = Shader.PropertyToID("_ContactShadowTextureUAV");

        public ContactShadowMapPass(RenderPassEvent rpe, ContactShadows contactShadows)
        {
            renderPassEvent = rpe;
            m_ContactShadows = contactShadows;
            m_ContactShadowComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Scripts/ContactShadow/Res/ContactShadows.compute");
            m_DeferredContactShadowKernel = m_ContactShadowComputeShader.FindKernel("ContactShadowMap");
            m_ContactShadowMapProfile = new ProfilingSampler(m_ContactShadowMapProfileTag);
        }

        public void UpdateContaceShadowParams(ContactShadows contactShadows)
        {
            m_ContactShadows = contactShadows;
            if (m_ContactShadows.enable)
                Shader.EnableKeyword("_CONTACT_SHADOW");
            else
                Shader.DisableKeyword("_CONTACT_SHADOW");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!m_ContactShadows.enable)
                return;
            
            var cameraData = renderingData.cameraData;
            var camera = renderingData.cameraData.camera;

            if (m_ContactShadowMap == null || m_ContactShadowMap.height != camera.pixelHeight || m_ContactShadowMap.width != camera.pixelWidth)
            {
                if (m_ContactShadowMap != null)
                    m_ContactShadowMap.Release();
                
                m_ContactShadowMap = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, GraphicsFormat.R8_UNorm);
                m_ContactShadowMap.name = "contactShadowMap";
                m_ContactShadowMap.useMipMap = false;
                m_ContactShadowMap.autoGenerateMips = false;
                m_ContactShadowMap.enableRandomWrite = true;
                m_ContactShadowMap.wrapMode = TextureWrapMode.Clamp;
                m_ContactShadowMap.filterMode = FilterMode.Point;
                m_ContactShadowMap.Create();
                
                Shader.SetGlobalTexture("_ContactShadowMap", m_ContactShadowMap);
            }
            
            float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance, 0.0f, m_ContactShadows.maxDistance);
            float contactShadowFadeEnd = m_ContactShadows.maxDistance;
            float contactShadowOneOverFadeRange = 1.0f / Mathf.Max(1e-6f, contactShadowRange);

            float contactShadowMinDist = Mathf.Min(m_ContactShadows.minDistance, contactShadowFadeEnd);
            float contactShadowFadeIn = Mathf.Clamp(m_ContactShadows.fadeInDistance, 1e-6f, contactShadowFadeEnd);
            
            var params1 = new Vector4(m_ContactShadows.length, m_ContactShadows.distanceScaleFactor, contactShadowFadeEnd, contactShadowOneOverFadeRange);
            var params2 = new Vector4(0, contactShadowMinDist, contactShadowFadeIn, m_ContactShadows.rayBias * 0.01f);
            var params3 = new Vector4(m_ContactShadows.sampleCount, m_ContactShadows.thicknessScale * 10.0f, Time.renderedFrameCount%8, 0.0f);
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ContactShadowMapProfileTag);
            using (new ProfilingScope(cmd, m_ContactShadowMapProfile))
            {
                cmd.SetComputeVectorParam(m_ContactShadowComputeShader, st_ContactShadowParamsParametersID, params1);
                cmd.SetComputeVectorParam(m_ContactShadowComputeShader, st_ContactShadowParamsParameters2ID, params2);
                cmd.SetComputeVectorParam(m_ContactShadowComputeShader, st_ContactShadowParamsParameters3ID, params3);
                cmd.SetComputeTextureParam(m_ContactShadowComputeShader, m_DeferredContactShadowKernel, st_ContactShadowTextureUAVID, m_ContactShadowMap);
                
                cmd.DispatchCompute(m_ContactShadowComputeShader, m_DeferredContactShadowKernel, Mathf.CeilToInt(camera.pixelWidth/8.0f), Mathf.CeilToInt(camera.pixelHeight/8.0f), 1);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
