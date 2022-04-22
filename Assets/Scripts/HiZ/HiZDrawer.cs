using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

namespace HiZRunTime
{
    public class HiZDrawer : ScriptableRendererFeature
    {
        private HiZDrawPass m_hizDrawPass;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public int renderPassEventOffset = 1;
        public string renderPassName = "Universal Forward";
        
        public override void Create()
        {
            m_hizDrawPass = new HiZDrawPass(renderPassEvent+renderPassEventOffset, renderPassName);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_hizDrawPass);
        }
    }

    public class HiZDrawPass : ScriptableRenderPass
    {
        private string passName = "Universal Forward";
        
        public HiZDrawPass(RenderPassEvent rpe, string passName)
        {
            this.renderPassEvent = rpe;
            this.passName = passName;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (HiZMgr.Instance != null)
                HiZMgr.Instance.ExecuteDraw(context, renderPassEvent, ref renderingData, passName);
        }
    }
}

