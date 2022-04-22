using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

namespace HiZRunTime
{
    public class HiZDepthGenerater : ScriptableRendererFeature
    {
        private HiZDepthGeneratePass m_hizCullTestPass;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        public int renderPassEventOffset = -1;
        
        public override void Create()
        {
            m_hizCullTestPass = new HiZDepthGeneratePass(renderPassEvent+renderPassEventOffset);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_hizCullTestPass);
        }
    }

    public class HiZDepthGeneratePass : ScriptableRenderPass
    {
        public HiZDepthGeneratePass(RenderPassEvent rpe)
        {
            this.renderPassEvent = rpe;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (HiZMgr.Instance != null)
                HiZMgr.Instance.ExecuteDepthGenerate(context, renderPassEvent, ref renderingData);
        }
    }
}
