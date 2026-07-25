using System.Runtime.InteropServices;
using TheEngine.Resources;
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

    private Vector4 outlineColorVec;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct OutlineMaterialData_t
    {
        public Vector4 outlineColor;
        public BindlessTextureId outlineTexIndex;          // bindless index of the blurred outline RT
        public BindlessTextureId outlineTexUnBlurredIndex; // bindless index of the sharp outline RT
        public BindlessTextureId mainTexIndex;             // bindless index of the scene image being post-processed
        public int padding0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ReplacementMaterialData_t
    {
        public Vector4 mesh_color;
        public float alphaTest;
        public BindlessTextureId texture1Index;
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
        outlineColorVec = outlineColor.ToVector4(); // the three texture indices are filled per frame in RenderPostprocess

        RT = new ScreenRenderTexture(engine);
        RT_downscaled = new ScreenRenderTexture(engine, 0.25f);
    }

    public void Render(List<Entity?> renderers)
    {
        RT.Update();
        RT_downscaled.Update();
        var entityManager = engine.EntityManager;
        engine.RenderManager.ActivateRenderTexture(RT.Texure, Color4.TransparentBlack);

        foreach (var maybeEntity in renderers)
        {
            if (maybeEntity is not { } entity)
                continue;
            RenderRecursive(entityManager, entity);
        }

        // through the render manager, so the blit is recorded in order with the draws above
        // (the texture-manager blit executes immediately and would copy last frame's content)
        engine.RenderManager.BlitRenderTextures(RT.Texure, RT_downscaled.Texure);
        engine.RenderManager.ActivateDefaultRenderTexture();
    }

    // Draws the entity's own mesh renderers, then the whole hierarchy below it (Relationship
    // component): mounts, item attachments etc. are child entities with their own renderers
    // and must glow together with the selected object.
    private void RenderRecursive(IEntityManager entityManager, Entity entity)
    {
        RenderEntityRenderers(entityManager, entity);
        var relationship = entityManager.GetComponent<Relationship>(entity);
        for (var child = relationship.FirstChild; child != Entity.Empty;
             child = entityManager.GetComponent<Relationship>(child).NextSibling)
            RenderRecursive(entityManager, child);
    }

    private void RenderEntityRenderers(IEntityManager entityManager, Entity entity)
    {
        // empty when the entity has no MeshRenderer array at all (text labels, group nodes, ...)
        var renderers_ = entityManager.GetArrayComponents<MeshRenderer>(entity);
        if (renderers_.Length == 0)
            return;

        var localToWorld = entityManager.GetComponent<LocalToWorld>(entity);
        for (int j = 0; j < renderers_.Length; ++j)
        {
            ref var renderer = ref renderers_[j];
            if (renderer.Hidden) // inactive geosets aren't on screen, so they must not glow either
                continue;
            var oldMaterial = engine.MaterialManager.GetMaterialByHandle(renderer.MaterialHandle);
            var oldCullMode = oldMaterial.Pipeline.Description.RasterizerState.CullMode;

            Material<ReplacementMaterialData_t> material;
            ReplacementMaterialData_t replacementData = default;
            replacementData.mesh_color = new Vector4(1, 0, 0, 1);
            if (oldMaterial is Material<WmoManager.WmoMaterialData> wmoMaterial)
            {
                material = replacementMaterialWmo[(int)oldCullMode];
                replacementData.alphaTest = wmoMaterial.MaterialData.alphaTest;
                // texture1 is sampled bindlessly; copy the slot index straight from the source material.
                replacementData.texture1Index = wmoMaterial.MaterialData.texture1Index;
            }
            else if (oldMaterial is Material<MdxManager.MdxMaterialData> m2Material)
            {
                material = replacementMaterialM2[(int)oldCullMode];
                replacementData.alphaTest = m2Material.MaterialData.alphaTest;
                // texture1 is sampled bindlessly; copy the slot index straight from the source material.
                replacementData.texture1Index = m2Material.MaterialData.texture1Index;
            }
            else
                continue; // a child with a foreign material type (water, decals, ...) - just skip it

            material.SetMaterialData(ref replacementData);

            engine.RenderManager.Render(renderer.MeshHandle, material.Handle, ShaderPassType.Forward, renderer.SubMeshId, localToWorld.Matrix, localToWorld.Inverse, instanceInt: renderer.InstanceData);
        }
    }

    public void RenderPostprocess(IRenderManager context, ITexture currentImage)
    {
        // RT / RT_downscaled are left in ShaderRead by BlitRenderTextures (see Render), currentImage
        // is barriered to ShaderRead by the postprocess loop - so all three are safe to sample bindlessly.
        OutlineMaterialData_t data = new()
        {
            outlineColor = outlineColorVec,
            outlineTexIndex = engine.TextureManager.GetBindlessIndex(RT_downscaled.Texure),
            outlineTexUnBlurredIndex = engine.TextureManager.GetBindlessIndex(RT.Texure),
            mainTexIndex = engine.TextureManager.GetBindlessIndex(currentImage),
        };
        outlineMaterial.SetMaterialData(ref data);
        context.RenderFullscreenPlane(outlineMaterial);
    }

    public void Dispose()
    {
        RT.Dispose();
        RT_downscaled.Dispose();
    }
}