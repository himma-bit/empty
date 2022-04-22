#if UNITY_2019_1_OR_NEWER && !EMBEDDED
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace VisualDesignCafe.Nature
{
    public class AOT
    {
        [Preserve]
        public void Include()
        {
            new Interaction.InteractionEntryPoint().OnBeginFrameRendering<ScriptableRenderContext, Camera[]>();
            new Overlay.OverlayEntryPoint().OnBeginFrameRendering<ScriptableRenderContext, Camera[]>();
        }
    }
}
#endif