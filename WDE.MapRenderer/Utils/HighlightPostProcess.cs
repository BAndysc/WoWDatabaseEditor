using System.Runtime.InteropServices;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.Module.Attributes;

namespace WDE.MapRenderer.Utils;

[AutoRegister]
public class HighlightPostProcess : IPostProcess, System.IDisposable
{
    private readonly Engine engine;
    private Material<ReplacementMaterialData_t> replacementMaterialM2 = null!;
    private Material<ReplacementMaterialData_t> replacementMaterialWmo = null!;
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
        outlineMaterial = engine.MaterialManager.CreateMaterial<OutlineMaterialData_t>("data/outline.json");
        outlineMaterial.BlendingEnabled = false;
        outlineMaterial.SourceBlending = Blending.One;
        outlineMaterial.DestinationBlending = Blending.Zero;
        outlineMaterial.DepthTesting = DepthCompare.Always;
        OutlineMaterialData_t data = default;
        data.outlineColor = outlineColor.ToVector4();
        outlineMaterial.SetMaterialData(ref data);

        RT = new ScreenRenderTexture(engine);
        RT_downscaled = new ScreenRenderTexture(engine, 0.25f);
        
        replacementMaterialM2 = engine.MaterialManager.CreateMaterial<ReplacementMaterialData_t>("data/unlit_flat_m2.json");
        replacementMaterialWmo = engine.MaterialManager.CreateMaterial<ReplacementMaterialData_t>("data/unlit_flat_wmo.json");
    }

    public void Render(IReadOnlyList<Entity>? renderers)
    {
        RT.Update();
        RT_downscaled.Update();
        var entityManager = engine.EntityManager;
        engine.RenderManager.ActivateRenderTexture(RT.Texure, Color4.TransparentBlack);

        if (renderers != null)
        {
            foreach (var entity in renderers)
            {
                var localToWorld = entityManager.GetComponent<LocalToWorld>(entity);
                var renderer = entityManager.GetComponent<MeshRenderer>(entity);
                var instanceData = entityManager.GetManagedComponent<MaterialInstanceRenderData>(entity);

                var oldMaterial = engine.MaterialManager.GetMaterialByHandle(renderer.MaterialHandle);
                var bones = instanceData.GetBuffer("boneMatrices");
                Material<ReplacementMaterialData_t> material;
                ReplacementMaterialData_t replacementData = default;
                replacementData.mesh_color = new Vector4(1, 0, 0, 1);
                if (oldMaterial is Material<WmoManager.WmoMaterialData> wmoMaterial)
                {
                    material = replacementMaterialWmo;
                    replacementData.alphaTest = wmoMaterial.MaterialData.alphaTest;
                    material.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                }
                else if (oldMaterial is Material<MdxManager.MdxMaterialData> m2Material)
                {
                    material = replacementMaterialM2;
                    replacementData.alphaTest = m2Material.MaterialData.alphaTest;
                    replacementMaterialM2.SetBuffer("boneMatrices", bones);
                    material.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                }
                else
                    throw new Exception("Unknown material type");

                material.Culling = oldMaterial.Culling;
                material.SetMaterialData(ref replacementData);

                engine.RenderManager.Render(renderer.MeshHandle, material.Handle, renderer.SubMeshId, localToWorld.Matrix, localToWorld.Inverse);
            }
        }
        
        engine.TextureManager.BlitRenderTextures(RT.Texure, RT_downscaled.Texure);
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