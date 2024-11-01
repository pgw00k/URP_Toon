using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class RenderFrontHairShadowMaskFeature : ScriptableRendererFeature
{
    private RenderFrontHairShadowMaskPass renderFrontHairMaskPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderFrontHairMaskPass);
    }

    public override void Create()
    {
        renderFrontHairMaskPass = new RenderFrontHairShadowMaskPass();
        renderFrontHairMaskPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }

    class RenderFrontHairShadowMaskPass : ScriptableRenderPass
    {
        static int maskId = Shader.PropertyToID("_HairShadowMask");
        static string keyword = "_HAIRSHADOWMASK";
        ShaderTagId maskTag = new ShaderTagId("HairShadowMask");

#if UNITY_2023
        static RTHandle maskRTHandle;
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
                // 为RTHandle分配资源
                RenderingUtils.ReAllocateIfNeeded(
                    ref maskRTHandle,
                    new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.R16),
                    FilterMode.Point
                );

            // 将RTHandle作为渲染目标
            ConfigureTarget(maskRTHandle);

            // 由于在Unity2023.2.10f1（其他版本未测试）中，Scene场景中的浮动工具条也算作场景的一部分
            // 这个工具条会受到后处理的影响，如果Clear的话，会导致工具条直接变成黑色
            // 这里暂且修改成只在Playing模式下进行Clear
            if(Application.isPlaying) 
            {
                ConfigureClear(ClearFlag.Color, Color.black);
            }

            // 将RTHandle绑定到Shader属性ID上
            cmd.SetGlobalTexture(maskId, maskRTHandle);

            // 设置着色器关键字
            CoreUtils.SetKeyword(cmd, keyword, true);
        }
#else
            static RenderTargetIdentifier mask_idt = new RenderTargetIdentifier(maskId);
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(maskId, new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.R16),FilterMode.Point);
            ConfigureTarget(mask_idt);
            ConfigureClear(ClearFlag.Color, Color.black);
            CoreUtils.SetKeyword(cmd, keyword, true);
        }
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(maskTag, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

#if UNITY_2023

#endif

        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(maskId);
            cmd.DisableShaderKeyword(keyword);

#if UNITY_2023
            if(maskRTHandle!=null)
            {
                maskRTHandle.Release();
                maskRTHandle = null;
            }
#endif
        }

    }
}
