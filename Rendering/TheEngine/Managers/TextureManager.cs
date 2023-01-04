#if DEBUG
#define DEBUG_CREATE_CALLSTACK
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
    internal class TextureManager : ITextureManager, IDisposable
    {
        private readonly Engine engine;
        private Dictionary<string, TextureHandle> texturesByPath;
        private Dictionary<TextureHandle, int> byPathReferencesCount = new();
        #if DEBUG_CREATE_CALLSTACK
        private Dictionary<ITexture, System.Diagnostics.StackTrace> createCallStack = new();
        #endif
        
        public TextureHandle EmptyTexture { get; private set; }
        private ITexture emptyTextureImpl { get; set; }
        
        private List<ITexture?> allTextures;

        internal TextureManager(Engine engine)
        {
            texturesByPath = new Dictionary<string, TextureHandle>();
            allTextures = new List<ITexture?>();
            this.engine = engine;

            EmptyTexture = CreateTexture(new uint[] { 0xFFFFFFFF }, 1, 1);
            emptyTextureImpl = GetTextureByHandle(EmptyTexture)!;
        }

        internal ITexture? this[TextureHandle handle]
        {
            get => handle.Handle == 0 ? null : allTextures[handle.Handle - 1];
            set => allTextures[handle.Handle - 1] = value;
        }
        
        public void Dispose()
        {
            emptyTextureImpl.Dispose();
            foreach (var tex in allTextures)
            {
                if (tex == null || tex == emptyTextureImpl)
                    continue;
                #if DEBUG_CREATE_CALLSTACK
                Console.WriteLine("Texture not disposed! Created: " + createCallStack[tex].ToString());
                #else
                Console.WriteLine("Texture not disposed!");
                #endif
                tex.Dispose();
            }
            allTextures.Clear();
            texturesByPath.Clear();
#if DEBUG_CREATE_CALLSTACK
            texturesByPath.Clear();
#endif
        }
        
        private TextureHandle AllocHandle() => new TextureHandle(allTextures.Count + 1);

        public TextureHandle CreateDummyHandle()
        {
            return AddTexture(emptyTextureImpl);
        }

        private TextureHandle AddTexture(ITexture texture)
        {
            var textureHandle = AllocHandle();
            allTextures.Add(texture);
#if DEBUG_CREATE_CALLSTACK
            if (texture != emptyTextureImpl)
                createCallStack[texture] = new System.Diagnostics.StackTrace(2, true);
#endif
            return textureHandle;
        }

        public void DisposeTexture(TextureHandle handle)
        {
            if (handle.Handle == 0)
                return;

            if (byPathReferencesCount.TryGetValue(handle, out var refCount))
            {
                if (refCount == 1)
                    byPathReferencesCount.Remove(handle);
                else
                {
                    byPathReferencesCount[handle]--;
                    return;
                }
            }
            
            var tex = GetTextureByHandle(handle);
            if (tex != emptyTextureImpl)
                tex?.Dispose();
#if DEBUG_CREATE_CALLSTACK
            if (tex != null)
               createCallStack.Remove(tex);
#endif
            this[handle] = null;
        }
        
        public void ReplaceHandles(TextureHandle old, TextureHandle @new)
        {
            var oldTexture = GetTextureByHandle(old);
            var newTexture = GetTextureByHandle(@new);
            this[old] = newTexture;
            if (oldTexture != emptyTextureImpl)
                oldTexture.Dispose();
#if DEBUG_CREATE_CALLSTACK
            createCallStack.Remove(oldTexture);
#endif
            this[@new] = null;
        }
        
        public TextureHandle LoadTexture(string path)
        {
            if (texturesByPath.TryGetValue(path, out var handle))
            {
                if (this[handle] != null)
                {
                    byPathReferencesCount[handle]++;
                    return handle;
                }
                else
                {
                    texturesByPath.Remove(path);
                }
            }

            using Image<Rgba32> image = Image.Load<Rgba32>(path);
            Rgba32[] array = new Rgba32[image.Width * image.Height];
            image.ProcessPixelRows(x =>
            {
                for (int i = 0; i < x.Height; ++i)
                    x.GetRowSpan(i).CopyTo(array.AsSpan(i * x.Width));
            });

            var textureHandle = CreateTexture(new Rgba32[][]{array}, image.Width, image.Height, true);
            texturesByPath.Add(path, textureHandle);
            byPathReferencesCount.Add(textureHandle, 1);
            return textureHandle;
        }
        
        public TextureHandle CreateTexture(Vector4[] pixels, int width, int height)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels);
            return AddTexture(texture);
        }

        public TextureHandle CreateTexture(float[] pixels, int width, int height)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels);
            return AddTexture(texture);
        }

        public TextureHandle CreateTexture(uint[]? pixels, int width, int height, TextureFormat format = TextureFormat.R8G8B8A8)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, format);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateTexture(Rgba32[][] pixels, int width, int height, bool generateMips)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, generateMips);
            return AddTexture(texture);
        }
        
        public unsafe TextureHandle CreateTexture(Rgba32* pixels, int width, int height, bool generateMips)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, generateMips);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateTextureArray(Rgba32[][][] textures, int width, int height)
        {
            var texture = engine.Device.CreateTextureArray(width, height, textures);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateRenderTexture(int width, int height, int colorAttachments = 1)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            var texture = engine.Device.CreateRenderTexture(width, height, colorAttachments);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateRenderTextureWithDepth(int width, int height, out TextureHandle depthTexture, int colorAttachments = 1)
        {
            depthTexture = CreateTexture(null, width, height, TextureFormat.DepthComponent);
            var texture = engine.Device.CreateRenderTexture(width, height, colorAttachments, (Texture)GetTextureByHandle(depthTexture)!);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateRenderTextureWithColorAndDepth(int width, int height, out TextureHandle colorTexture, out TextureHandle depthTexture)
        {
            depthTexture = CreateTexture(null, width, height, TextureFormat.DepthComponent);
            colorTexture = CreateTexture(null, width, height, TextureFormat.R8G8B8A8);
            var texture = engine.Device.CreateRenderTexture((Texture)GetTextureByHandle(colorTexture)!, (Texture)GetTextureByHandle(depthTexture)!);
            return AddTexture(texture);
        }
        
        public void ScreenshotRenderTexture(TextureHandle handle, string fileName, int colorAttachmentIndex = 0)
        {
            var rt = GetTextureByHandle(handle) as RenderTexture;
            rt.ActivateSourceFrameBuffer(colorAttachmentIndex);
            Rgba32[] pixels = new Rgba32[rt.Width * rt.Height];
            engine.Device.device.ReadPixels(0, 0, rt.Width, rt.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.AsSpan());
            using Image<Rgba32> image = Image.LoadPixelData<Rgba32>(pixels, rt.Width, rt.Height);
            image.SaveAsPng(fileName);
        }

        internal ITexture? GetTextureByHandle(TextureHandle textureHandle)
        {
            return this[textureHandle];
        }
        
        public void SetFiltering(TextureHandle handle, FilteringMode mode)
        {
            GetTextureByHandle(handle).SetFiltering(mode);
        }
        
        public void SetWrapping(TextureHandle handle, WrapMode mode)
        {
            GetTextureByHandle(handle).SetWrapping(mode);
        }

        public void BlitFramebuffers(TextureHandle src, TextureHandle dst, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, ClearBufferMask mask, BlitFramebufferFilter filter)
        {
            var srcTex = GetTextureByHandle(src) as RenderTexture;
            var dstTex = GetTextureByHandle(dst) as RenderTexture;
            
            srcTex!.ActivateSourceFrameBuffer(0);
            dstTex!.ActivateRenderFrameBuffer();
            engine.Device.device.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1,  dstX0, dstY0,  dstX1,  dstY1, mask, filter);
        }
        
        public void BlitRenderTextures(TextureHandle src, TextureHandle dst)
        {
            var srcTex = GetTextureByHandle(src) as RenderTexture;
            var dstTex = GetTextureByHandle(dst) as RenderTexture;
            
            srcTex!.ActivateSourceFrameBuffer(0);
            dstTex!.ActivateRenderFrameBuffer();
            engine.Device.device.BlitFramebuffer(0, 0, srcTex.Width, srcTex.Height, 0, 0, dstTex.Width, dstTex.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            engine.Device.device.BlitFramebuffer(0, 0, srcTex.Width, srcTex.Height, 0, 0, dstTex.Width, dstTex.Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        }

        public bool TextureExists(TextureHandle handle)
        {
            return allTextures.Count > handle.Handle && allTextures[handle.Handle] != null;
        }
    }
}
