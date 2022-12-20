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
    private Material replacementMaterialM2 = null!;
    private Material replacementMaterialWmo = null!;
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
        
        replacementMaterialM2 = engine.MaterialManager.CreateMaterial("data/unlit_flat_m2.json");
        replacementMaterialM2.SetUniform("mesh_color", new Vector4(1, 0, 0, 1));
        replacementMaterialWmo = engine.MaterialManager.CreateMaterial("data/unlit_flat_wmo.json");
        replacementMaterialWmo.SetUniform("mesh_color", new Vector4(1, 0, 0, 1));
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
                var bones = instanceData.GetBuffer("boneMatrices");
                Material material;
                if (bones != null)
                {
                    material = replacementMaterialM2;
                    replacementMaterialM2.SetBuffer("boneMatrices", bones);
                }
                else
                {
                    material = replacementMaterialWmo;
                }
                material.Culling = oldMaterial.Culling;
                material.SetUniform("alphaTest", oldMaterial.GetUniformFloat("alphaTest"));
                material.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                    
                engine.RenderManager.Render(renderer.MeshHandle, material.Handle, renderer.SubMeshId, localToWorld.Matrix, localToWorld.Inverse);
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