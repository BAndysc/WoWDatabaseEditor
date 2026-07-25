using System.Runtime.InteropServices;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using Veldrid;

namespace TheEngine.Utils;

public struct AmbientOcclusion : IComponentData
{
    /// <summary>When false the pass is a cheap pass-through (no darkening).</summary>
    public bool Enabled = true;
    /// <summary>Sampling radius in view-space units.</summary>
    public float Radius = 0.5f;
    /// <summary>Darkening strength (0 = none).</summary>
    public float Intensity = 1.0f;
    /// <summary>Contrast curve applied to the AO term (1 = linear).</summary>
    public float Power = 2.0f;
    /// <summary>Depth bias to avoid self-occlusion acne.</summary>
    public float Bias = 0.025f;
    public float MinDist = 30f;
    public float MaxDist = 60f;
    private StaticReference postProcessGcHandle;

    public AmbientOcclusion()
    {
    }

    // picked up automatically by ComponentTypeData<AmbientOcclusion> via naming convention
    public static void OnAdded(Engine engine, Entity entity, ref AmbientOcclusion component)
    {
        var postProcess = new AmbientOcclusionPostProcess(engine, entity);
        engine.RenderManager.AddPostprocess(postProcess);
        component.postProcessGcHandle = postProcess.GetStaticReference();
    }

    public static void OnRemoved(Engine engine, Entity entity, ref AmbientOcclusion component)
    {
        if (component.postProcessGcHandle.IsEmpty)
            return;
        if (Static.TryGet(component.postProcessGcHandle, out var value) && value is AmbientOcclusionPostProcess postProcess)
        {
            engine.RenderManager.RemovePostprocess(postProcess);
            postProcess.Dispose();
        }
        component.postProcessGcHandle.Free();
        component.postProcessGcHandle = default;
    }
}

/// <summary>
/// Screen-space ambient occlusion as a full-screen post-process. Reconstructs view-space
/// position/normal from the opaque depth buffer (no normal G-buffer needed) and multiplies the
/// composited image by the resulting occlusion term. Lifecycle is owned by the <see cref="AmbientOcclusion"/>
/// ECS component (see its OnAdded/OnRemoved) - nothing else needs to instantiate this.
/// </summary>
internal class AmbientOcclusionPostProcess : IPostProcess, System.IDisposable
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct SsaoData
    {
        public Matrix invProjection; // 64
        public Matrix projection;    // 64
        public Vector4 aoParams;     // 16: radius, intensity, power, bias
        public Vector4 misc;         // 16: min dist, max dist, enabled, _
        public unsafe fixed float samples[16 * 4];
        public BindlessTextureId mainTexIndex;     // ivec4 texIndices in the shader
        public BindlessTextureId depthTexIndex;
        public int pad0;
        public int pad1;
    }

    private readonly Engine engine;
    private readonly Entity entity;
    private readonly Material<SsaoData> material;
    private static float[] kernel = GenerateKernel();

    public AmbientOcclusionPostProcess(Engine engine, Entity entity)
    {
        this.engine = engine;
        this.entity = entity;

        var shader = engine.ShaderManager.LoadShader("internalShaders/ssao.json");
        var pipeline = engine.PipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = RasterizerStateDescription.CullNone,
        }, false);
        material = engine.MaterialManager.CreateMaterial<SsaoData>(pipeline);
    }

    public unsafe void RenderPostprocess(IRenderManager context, ITexture currentImage)
    {
        ref var data = ref engine.EntityManager.GetComponent<AmbientOcclusion>(entity);

        var camera = engine.CameraManager.MainCamera;
        var proj = camera.ProjectionMatrix;
        Matrix.Invert(proj, out var invProj);

        SsaoData ssaoData = default;
        ssaoData.projection = proj;
        ssaoData.invProjection = invProj;
        ssaoData.aoParams = new Vector4(data.Radius, data.Intensity, data.Power, data.Bias);
        ssaoData.misc = new Vector4(data.MinDist, data.MaxDist, data.Enabled ? 1 : 0, 0);
        for (int i = 0; i < kernel.Length; ++i)
        {
            ssaoData.samples[i] = kernel[i];
        }
        ssaoData.mainTexIndex = engine.TextureManager.GetBindlessIndex(currentImage);
        ssaoData.depthTexIndex = engine.TextureManager.GetBindlessIndex(context.DepthTexture);
        material.SetMaterialData(ref ssaoData);

        context.RenderFullscreenPlane(material);
    }

    public static float[] GenerateKernel()
    {
        // 16 samples * 4 floats per sample (x, y, z, w)
        float[] ssaoKernel = new float[16 * 4];
        Random random = new Random();

        for (int i = 0; i < 16; ++i)
        {
            // 1. Generate random sample in a hemisphere (Z goes from 0 to 1)
            float x = (float)random.NextDouble() * 2.0f - 1.0f;
            float y = (float)random.NextDouble() * 2.0f - 1.0f;
            float z = (float)random.NextDouble();

            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            if (length > 0.0001f)
            {
                x /= length;
                y /= length;
                z /= length;
            }

            float distance = (float)random.NextDouble();
            x *= distance;
            y *= distance;
            z *= distance;

            // 3. Scale samples to cluster near the origin
            float scale = (float)i / 16.0f;
            scale = Lerp(0.1f, 1.0f, scale * scale);

            x *= scale;
            y *= scale;
            z *= scale;

            int index = i * 4;
            ssaoKernel[index] = x;
            ssaoKernel[index + 1] = y;
            ssaoKernel[index + 2] = z;
            ssaoKernel[index + 3] = 0.0f;    // W (padding/bias)
        }

        return ssaoKernel;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public void Dispose()
    {
    }
}
