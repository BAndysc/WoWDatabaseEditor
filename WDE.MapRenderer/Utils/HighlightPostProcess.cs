using System.Runtime.InteropServices;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;
using Veldrid;
using WDE.MapRenderer.Managers;
using WDE.Module.Attributes;

namespace WDE.MapRenderer.Utils;

[AutoRegister]
public class HighlightPostProcess : IPostProcess, System.IDisposable
{
    private readonly Engine engine;
    private Material<ReplacementMaterialData_t>[] replacementMaterialM2 = new Material<ReplacementMaterialData_t>[(int)FaceCullMode.None + 1];
    private Material<ReplacementMaterialData_t>[] replacementMaterialWmo = new Material<ReplacementMaterialData_t>[(int)FaceCullMode.None + 1];
    private Material<OutlineMaterialData_t> outlineMaterial = null!;

    private ScreenRenderTexture RT;
    private ScreenRenderTexture RT_downscaled;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct OutlineMaterialData_t
    {
        public Vector4 outlineColor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ReplacementMaterialData_t
    {
        public Vector4 mesh_color;
        public float alphaTest;
        public int padding0;
        public int padding1;
        public int padding2;
    };

    public HighlightPostProcess(Engine engine, Color outlineColor)
    {
        this.engine = engine;

        var m2ReplacementShader = engine.ShaderManager.LoadShader("data/unlit_flat_m2.json");
        var wmoReplacementShader = engine.ShaderManager.LoadShader("data/unlit_flat_wmo.json");
        var outlineShader = engine.ShaderManager.LoadShader("data/outline.json");
        var blitDownscaleShader = engine.ShaderManager.LoadShader("internalShaders/blit.json");

        foreach (var cullMode in new[] { FaceCullMode.Back, FaceCullMode.Front, FaceCullMode.None })
        {
            var m2Pipeline = this.engine.PipelineManager.CreatePipeline(m2ReplacementShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = new RasterizerStateDescription()
                {
                    CullMode = cullMode,
                    DepthClipEnabled = true
                },
            }, false);

            var wmoPipeline = this.engine.PipelineManager.CreatePipeline(wmoReplacementShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = new RasterizerStateDescription()
                {
                    CullMode = cullMode,
                    DepthClipEnabled = true
                }
            }, false);

            replacementMaterialM2[(int)cullMode] = engine.MaterialManager.CreateMaterial<ReplacementMaterialData_t>(m2Pipeline);
            replacementMaterialWmo[(int)cullMode] = engine.MaterialManager.CreateMaterial<ReplacementMaterialData_t>(wmoPipeline);
        }

        var outlinePipeline = this.engine.PipelineManager.CreatePipeline(outlineShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = DepthStencilStateDescription.Disabled, // DepthCompare.Always;
            RasterizerState = RasterizerStateDescription.CullNone,
        }, false);

        var blitDownscaledPipeline = this.engine.PipelineManager.CreatePipeline(blitDownscaleShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = RasterizerStateDescription.CullNone,
        }, false);

        outlineMaterial = engine.MaterialManager.CreateMaterial<OutlineMaterialData_t>(outlinePipeline);
        OutlineMaterialData_t data = default;
        data.outlineColor = outlineColor.ToVector4();
        outlineMaterial.SetMaterialData(ref data);

        RT = new ScreenRenderTexture(engine);
        RT_downscaled = new ScreenRenderTexture(engine, 0.25f);
    }

    public void Render(IReadOnlyList<Entity?> renderers)
    {
        RT.Update();
        RT_downscaled.Update();
        var entityManager = engine.EntityManager;
        engine.RenderManager.ActivateRenderTexture(RT.Texure, Color4.TransparentBlack);

        foreach (var maybeEntity in renderers)
        {
            if (maybeEntity is not { } entity)
                continue;
            var localToWorld = entityManager.GetComponent<LocalToWorld>(entity);
            var renderers_ = entityManager.GetArrayComponents<MeshRenderer>(entity);
            var instanceData = entityManager.GetManagedComponent<MaterialInstanceRenderData>(entity);

            for (int j = 0; j < renderers_.Length; ++j)
            {
                ref var renderer = ref renderers_[j];
                var oldMaterial = engine.MaterialManager.GetMaterialByHandle(renderer.MaterialHandle);
                var oldCullMode = oldMaterial.Pipeline.Description.RasterizerState.CullMode;

                var bones = instanceData.GetBuffer("boneMatrices");
                Material<ReplacementMaterialData_t> material;
                ReplacementMaterialData_t replacementData = default;
                replacementData.mesh_color = new Vector4(1, 0, 0, 1);
                if (oldMaterial is Material<WmoManager.WmoMaterialData> wmoMaterial)
                {
                    material = replacementMaterialWmo[(int)oldCullMode];
                    replacementData.alphaTest = wmoMaterial.MaterialData.alphaTest;
                    material.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                }
                else if (oldMaterial is Material<MdxManager.MdxMaterialData> m2Material)
                {
                    material = replacementMaterialM2[(int)oldCullMode];
                    replacementData.alphaTest = m2Material.MaterialData.alphaTest;
                    material.SetBuffer("boneMatrices", bones);
                    material.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                }
                else
                    throw new Exception("Unknown material type");

                material.SetMaterialData(ref replacementData);

                engine.RenderManager.Render(renderer.MeshHandle, material.Handle, ShaderPassType.Forward, renderer.SubMeshId, localToWorld.Matrix, localToWorld.Inverse, instanceInt: renderer.InstanceData);

            }
        }
        
        // through the render manager, so the blit is recorded in order with the draws above
        // (the texture-manager blit executes immediately and would copy last frame's content)
        engine.RenderManager.BlitRenderTextures(RT.Texure, RT_downscaled.Texure);
        engine.RenderManager.ActivateDefaultRenderTexture();
    }

    public void RenderPostprocess(IRenderManager context, ITexture currentImage)
    {
        outlineMaterial.SetTexture("outlineTex", RT_downscaled.Texure);
        outlineMaterial.SetTexture("outlineTexUnBlurred", RT.Texure);
        outlineMaterial.SetTexture("_MainTex", currentImage);
        context.RenderFullscreenPlane(outlineMaterial);
    }

    public void Dispose()
    {
        RT.Dispose();
        RT_downscaled.Dispose();
    }
}