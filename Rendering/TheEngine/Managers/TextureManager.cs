#if DEBUG
// #define DEBUG_CREATE_CALLSTACK
#endif

using System;
using System.Collections.Generic;
using OpenGLBindings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    internal class Texture : ITexture
    {
        private readonly Engine engine;

        public Texture(Engine engine ,TextureHandle handle, INativeTexture nativeTexture)
        {
            this.engine = engine;
            Handle = handle;
            NativeTexture = nativeTexture;
        }

        ~Texture()
        {
            engine.textureManager.AddToDisposeList(this);
        }

        public TextureHandle Handle { get; }
        public int Width => NativeTexture.Width;
        public int Height => NativeTexture.Height;
        internal INativeTexture NativeTexture { get; }
    }

    internal class TextureManager : ITextureManager, IDisposable
    {
        private readonly Engine engine;
        private Dictionary<string, ITexture> texturesByPath;
        #if DEBUG_CREATE_CALLSTACK
        private Dictionary<TextureHandle, System.Diagnostics.StackTrace> createCallStack = new();
        #endif
        
        public ITexture EmptyTexture { get; private set; }
        private INativeTexture emptyTextureImpl { get; set; }
        private int disposeIndex = 0;
        private List<Texture>[] disposeLists = [new(), new()];
        private List<WeakReference<Texture>?> allTextures;

        internal TextureManager(Engine engine)
        {
            texturesByPath = new Dictionary<string, ITexture>();
            allTextures = new();
            this.engine = engine;

            EmptyTexture = CreateTexture(new uint[] { 0xFFFFFFFF }, 1, 1);
            emptyTextureImpl = ((Texture)EmptyTexture).NativeTexture;
        }

        internal ITexture? this[TextureHandle handle]
        {
            get
            {
                return handle.Handle == 0 ? null : allTextures[handle.Handle - 1] != null && allTextures[handle.Handle - 1].TryGetTarget(out var tex) ? tex : null;
            }
        }

        public void Dispose()
        {
            emptyTextureImpl.Dispose();
            foreach (var tex in allTextures)
            {
                if (tex == null || !tex.TryGetTarget(out var target))
                    continue;
                #if DEBUG_CREATE_CALLSTACK
                Console.WriteLine("Texture not disposed! Created: " + createCallStack[target.Handle].ToString());
                #else
                Console.WriteLine("Texture not disposed!");
                #endif
                target.NativeTexture.Dispose();
            }
            allTextures.Clear();
            texturesByPath.Clear();
#if DEBUG_CREATE_CALLSTACK
            texturesByPath.Clear();
#endif
        }
        
        private TextureHandle AllocHandle() => new TextureHandle(allTextures.Count + 1);

        private ITexture AddTexture(INativeTexture texture)
        {
            var handle = AllocHandle();
            var tex = new Texture(engine, handle, texture);
            allTextures.Add(new WeakReference<Texture>(tex));
#if DEBUG_CREATE_CALLSTACK
            if (texture != emptyTextureImpl)
                createCallStack[handle] = new System.Diagnostics.StackTrace(2, true);
#endif
            engine.statsManager.TextureBytes += (ulong)texture.SizeInBytes;
            return tex;
        }

        public void DisposeTexture(ITexture? tex)
        {
            if (tex == null)
                return;

            if (tex.Handle.Handle == 0)
                return;

            var texture = ((Texture)tex);

            if (texture.NativeTexture != emptyTextureImpl)
            {
                texture.NativeTexture.Dispose();
                engine.statsManager.TextureBytes -= (ulong)texture.NativeTexture.SizeInBytes;
            }
#if DEBUG_CREATE_CALLSTACK
            if (tex != null)
               createCallStack.Remove(tex.Handle);
#endif
            allTextures[tex.Handle.Handle - 1] = null;
        }

        public ITexture LoadTexture(string path)
        {
            if (texturesByPath.TryGetValue(path, out var handle))
            {
                return handle;
            }

            using Image<Rgba32> image = Image.Load<Rgba32>(path);
            Rgba32[] array = new Rgba32[image.Width * image.Height];
            image.ProcessPixelRows(x =>
            {
                for (int i = 0; i < x.Height; ++i)
                    x.GetRowSpan(i).CopyTo(array.AsSpan(i * x.Width));
            });

            var texture = CreateTexture(new Rgba32[][]{array}, image.Width, image.Height, true);
            texturesByPath.Add(path, texture);
            return texture;
        }
        
        public ITexture CreateTexture(Vector4[] pixels, int width, int height)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels);
            return AddTexture(texture);
        }

        public ITexture CreateTexture(float[] pixels, int width, int height)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels);
            return AddTexture(texture);
        }

        public ITexture CreateTexture(uint[]? pixels, int width, int height, TextureFormat format = TextureFormat.R8G8B8A8)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, format);
            return AddTexture(texture);
        }
        
        public ITexture CreateTexture(Rgba32[][] pixels, int width, int height, bool generateMips)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, generateMips);
            return AddTexture(texture);
        }
        
        public unsafe ITexture CreateTexture(Rgba32* pixels, int width, int height, bool generateMips)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, generateMips);
            return AddTexture(texture);
        }
        
        public ITexture CreateTextureArray(Rgba32[][][] textures, int width, int height)
        {
            var texture = engine.Device.CreateTextureArray(width, height, textures);
            return AddTexture(texture);
        }
        
        public ITexture CreateRenderTexture(int width, int height, int colorAttachments = 1)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            var texture = engine.Device.CreateRenderTexture(width, height, colorAttachments);
            return AddTexture(texture);
        }
        
        public ITexture CreateRenderTextureWithDepth(int width, int height, out ITexture depthTexture, int colorAttachments = 1)
        {
            depthTexture = CreateTexture(null, width, height, TextureFormat.DepthComponent);
            var texture = engine.Device.CreateRenderTexture(width, height, colorAttachments, (Texture2D)GetTextureByHandle(depthTexture.Handle)!);
            return AddTexture(texture);
        }
        
        public ITexture CreateRenderTextureWithColorAndDepth(int width, int height, out ITexture colorTexture, out ITexture depthTexture)
        {
            depthTexture = CreateTexture(null, width, height, TextureFormat.DepthComponent);
            colorTexture = CreateTexture(null, width, height, TextureFormat.R8G8B8A8);
            var texture = engine.Device.CreateRenderTexture((Texture2D)GetTextureByHandle(colorTexture.Handle)!, (Texture2D)GetTextureByHandle(depthTexture.Handle)!);
            return AddTexture(texture);
        }

        public ITexture CreateRenderTexture(ITexture colorTexture, ITexture depthTexture, ITexture colorTexture1)
        {
            var color = ((Texture)colorTexture).NativeTexture ?? throw new ArgumentException("Color texture handle is invalid.");
            var depth = ((Texture)depthTexture).NativeTexture ?? throw new ArgumentException("Depth texture handle is invalid.");
            var color1 =  colorTexture1 != default ? ((Texture)colorTexture1).NativeTexture : null;
            if (color1 != null && (color1.Width != color.Width || color1.Height != color.Height))
                throw new ArgumentException("Color texture and color1 texture must have the same dimensions.");
            if (color.Width != depth.Width || color.Height != depth.Height)
                throw new ArgumentException("Color texture and depth texture must have the same dimensions.");
            var texture = engine.Device.CreateRenderTexture((Texture2D)color, (Texture2D)depth, (Texture2D)color1);
            return AddTexture(texture);
        }
        
        public void ScreenshotRenderTexture(ITexture handle, string fileName, int colorAttachmentIndex = 0)
        {
            var rt = ((Texture)handle).NativeTexture as RenderTexture;
            rt.ActivateSourceFrameBuffer(colorAttachmentIndex);
            Rgba32[] pixels = new Rgba32[rt.Width * rt.Height];
            engine.Device.device.ReadPixels(0, 0, rt.Width, rt.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.AsSpan());
            using Image<Rgba32> image = Image.LoadPixelData<Rgba32>(pixels, rt.Width, rt.Height);
            image.SaveAsPng(fileName);
        }

        internal INativeTexture? GetTextureByHandle(TextureHandle handle)
        {
            return ((Texture)this[handle])?.NativeTexture;
        }
        
        public void SetFiltering(ITexture texture, FilteringMode mode)
        {
            ((Texture)texture).NativeTexture.SetFiltering(mode);
        }
        
        public void SetWrapping(ITexture texture, WrapMode mode)
        {
            ((Texture)texture).NativeTexture.SetWrapping(mode);
        }

        public void BlitFramebuffers(ITexture src, ITexture dst, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, ClearBufferMask mask, BlitFramebufferFilter filter)
        {
            var srcTex = ((Texture)src).NativeTexture as RenderTexture;
            var dstTex = ((Texture)dst).NativeTexture as RenderTexture;
            
            srcTex!.ActivateSourceFrameBuffer(0);
            dstTex!.ActivateRenderFrameBuffer();
            engine.Device.device.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1,  dstX0, dstY0,  dstX1,  dstY1, mask, filter);
        }
        
        public void BlitRenderTextures(ITexture src, ITexture dst)
        {
            var srcTex = ((Texture)src).NativeTexture as RenderTexture;
            var dstTex = ((Texture)dst).NativeTexture as RenderTexture;
            
            srcTex!.ActivateSourceFrameBuffer(0);
            dstTex!.ActivateRenderFrameBuffer();
            engine.Device.device.BlitFramebuffer(0, 0, srcTex.Width, srcTex.Height, 0, 0, dstTex.Width, dstTex.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            engine.Device.device.BlitFramebuffer(0, 0, srcTex.Width, srcTex.Height, 0, 0, dstTex.Width, dstTex.Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        }

        public bool TextureExists(TextureHandle handle)
        {
            return allTextures.Count >= handle.Handle && allTextures[handle.Handle - 1] != null;
        }

        internal void Update()
        {
            List<Texture> toDispose;
            lock (this)
            {
                toDispose = disposeLists[disposeIndex];
                disposeIndex = 1 - disposeIndex;
            }

            foreach (var texture in toDispose)
            {
                DisposeTexture(texture);
            }
            toDispose.Clear();
        }

        internal void AddToDisposeList(Texture texture)
        {
            lock (this)
            {
                if (texture.NativeTexture.NativeHandle == 0)
                    return;
                disposeLists[disposeIndex].Add(texture);
            }
        }
    }
}
