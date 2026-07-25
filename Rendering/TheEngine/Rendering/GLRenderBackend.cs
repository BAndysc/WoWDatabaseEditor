using OpenGLBindings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Entities;
using TheEngine.Managers;
using TheMaths;
using PixelFormat = OpenGLBindings.PixelFormat;

namespace TheEngine.Rendering;

/// <summary>
/// The OpenGL backend: a thin wrapper over the historical <see cref="TheDevice"/> resource
/// factory and <see cref="ImmediateCommandList"/> executor. Frozen as the reference
/// implementation - it gets deleted once the Vulkan backend matches it.
/// </summary>
internal class GLRenderBackend : IRenderBackend
{
    private readonly IDevice device;

    /// <summary>The legacy GL resource factory, still exposed for the GL window/panel hosts.</summary>
    internal TheDevice TheDevice { get; }

    public string Name => "OpenGL";

    public GLRenderBackend(IDevice device, IWindowHost windowHost)
    {
        this.device = device;
        TheDevice = new TheDevice(windowHost, device, false);
        TheDevice.Initialize();
    }

    public ICommandList CreateExecutor(TextureManager textureManager)
        => new ImmediateCommandList(TheDevice, textureManager);

    public void BeginFrame() => device.Begin();

    public void EndFrame()
    {
        // present is the GL window host's job (glFinish + SwapBuffers)
    }

    public INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged
        => TheDevice.CreateBuffer<T>(bufferType, size, format);

    public INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged
        => TheDevice.CreateBuffer<T>(bufferType, data, format);

    public INativeTexture CreateTexture(int width, int height, Rgba32[][] pixels, bool generateMips)
        => TheDevice.CreateTexture(width, height, pixels, generateMips);

    public unsafe INativeTexture CreateTexture(int width, int height, Rgba32* pixels, bool generateMips)
        => TheDevice.CreateTexture(width, height, pixels, generateMips);

    public INativeTexture CreateTexture(int width, int height, Vector4[] pixels)
        => TheDevice.CreateTexture(width, height, pixels);

    public INativeTexture CreateTexture(int width, int height, float[] pixels)
        => TheDevice.CreateTexture(width, height, pixels);

    public INativeTexture CreateTexture(int width, int height, uint[]? pixels, TextureFormat format)
        => TheDevice.CreateTexture(width, height, pixels, format);

    public INativeTexture CreateTextureArray(int width, int height, Rgba32[][][] pixels)
        => TheDevice.CreateTextureArray(width, height, pixels);

    public INativeTexture CreateRenderTexture(int width, int height, int colorAttachments = 1, INativeTexture? depthTexture = null)
        => TheDevice.CreateRenderTexture(width, height, colorAttachments, (Texture2D?)depthTexture);

    public INativeTexture CreateRenderTexture(INativeTexture colorAttachment, INativeTexture depthTexture, INativeTexture? colorAttachment1 = null)
        => TheDevice.CreateRenderTexture((Texture2D)colorAttachment, (Texture2D)depthTexture, (Texture2D?)colorAttachment1);

    public IShader LoadShader(string jsonPath, string[] includePaths)
        => new Shader(device, jsonPath, includePaths);

    public unsafe void OnMeshCreated(Mesh mesh)
    {
        // GL 4.1 has no separate vertex-format state: the universal vertex layout is
        // captured in a VAO against the mesh's concrete buffer objects
        mesh.VertexArrayObject = device.GenVertexArray();
        device.BindVertexArray(mesh.VertexArrayObject);
        mesh.VerticesBuffer!.Activate(0);
        mesh.IndicesBuffer!.Activate(0);
        int stride = sizeof(Structures.UniversalVertex);

        device.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, new IntPtr(0));
        device.EnableVertexAttribArray(0);
        device.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, new IntPtr(12));
        device.EnableVertexAttribArray(1);
        device.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(24));
        device.EnableVertexAttribArray(2);
        device.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(32));
        device.EnableVertexAttribArray(3);
        device.VertexAttribPointer(4, 4, VertexAttribPointerType.UnsignedByte, true, stride, new IntPtr(40));
        device.EnableVertexAttribArray(4);
        device.VertexAttribPointer(5, 4, VertexAttribPointerType.UnsignedByte, true, stride, new IntPtr(44));
        device.EnableVertexAttribArray(5);
        device.BindVertexArray(0);
    }

    public void ScreenshotRenderTexture(INativeTexture renderTexture, string fileName, int colorAttachmentIndex)
    {
        var rt = (RenderTexture)renderTexture;
        rt.ActivateSourceFrameBuffer(colorAttachmentIndex);
        Rgba32[] pixels = new Rgba32[rt.Width * rt.Height];
        device.ReadPixels(0, 0, rt.Width, rt.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.AsSpan());
        using Image<Rgba32> image = Image.LoadPixelData<Rgba32>(pixels, rt.Width, rt.Height);
        // GL reads rows bottom-up; the contract is a correctly oriented PNG
        image.Mutate(x => x.Flip(FlipMode.Vertical));
        image.SaveAsPng(fileName);
    }

    public void CollectDisposedResources() => device.DisposeBuffers();

    public long TotalBufferBytes => device.TotalBufferBytes;

    public void Dispose()
    {
        TheDevice.Dispose();
    }
}
