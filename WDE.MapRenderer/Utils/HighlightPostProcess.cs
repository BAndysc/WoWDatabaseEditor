using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;
using WDE.Module.Attributes;

namespace WDE.MapRenderer.Utils;

[AutoRegister]
public class HighlightPostProcess : IPostProcess, System.IDisposable
{
    private readonly Engine engine;
    private Material replacementMaterial = null!;
    private Material outlineMaterial = null!;

    private ScreenRenderTexture RT;
    private ScreenRenderTexture RT_downscaled;
    
    public HighlightPostProcess(Engine engine, Color outlineColor)
    {
        this.engine = engine;
        outlineMaterial = engine.MaterialManager.CreateMaterial("data/outline.json");
        outlineMaterial.BlendingEnabled = false;
        outlineMaterial.SourceBlending = Blending.One;
        outlineMaterial.DestinationBlending = Blending.Zero;
        outlineMaterial.DepthTesting = DepthCompare.Always;
        outlineMaterial.SetUniform("outlineColor", outlineColor.ToVector4());

        RT = new ScreenRenderTexture(engine);
        RT_downscaled = new ScreenRenderTexture(engine, 0.25f);
        
        replacementMaterial = engine.MaterialManager.CreateMaterial("data/unlit_flat_m2.json");
        replacementMaterial.SetUniform("mesh_color", new Vector4(1, 0, 0, 1));
    }

    public void Render(IReadOnlyList<Entity>? renderers)
    {
        RT.Update();
        RT_downscaled.Update();
        var entityManager = engine.EntityManager;
        engine.RenderManager.ActivateRenderTexture(RT, Color4.TransparentBlack);

        if (renderers != null)
        {
            foreach (var entity in renderers)
            {
                var localToWorld = entityManager.GetComponent<LocalToWorld>(entity);
                var renderer = entityManager.GetComponent<MeshRenderer>(entity);
                var instanceData = entityManager.GetManagedComponent<MaterialInstanceRenderData>(entity);

                var oldMaterial = engine.MaterialManager.GetMaterialByHandle(renderer.MaterialHandle);
                replacementMaterial.Culling = oldMaterial.Culling;
                replacementMaterial.SetUniform("alphaTest", oldMaterial.GetUniformFloat("alphaTest"));
                replacementMaterial.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                replacementMaterial.SetBuffer("boneMatrices", instanceData.GetBuffer("boneMatrices")!);
                engine.RenderManager.Render(renderer.MeshHandle, replacementMaterial.Handle, renderer.SubMeshId, localToWorld.Matrix, localToWorld.Inverse);
            }
        }
        
        engine.TextureManager.BlitRenderTextures(RT, RT_downscaled);
        engine.RenderManager.ActivateDefaultRenderTexture();
    }

    public void RenderPostprocess(IRenderManager context, TextureHandle currentImage)
    {
        outlineMaterial.SetTexture("outlineTex", RT_downscaled);
        outlineMaterial.SetTexture("outlineTexUnBlurred", RT);
        outlineMaterial.SetTexture("_MainTex", currentImage);
        context.RenderFullscreenPlane(outlineMaterial);
    }

    public void Dispose()
    {
        RT.Dispose();
        RT_downscaled.Dispose();
    }
}