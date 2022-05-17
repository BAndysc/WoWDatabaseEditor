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
        
        private List<ITexture?> allTextures;

        internal TextureManager(Engine engine)
        {
            texturesByPath = new Dictionary<string, TextureHandle>();
            allTextures = new List<ITexture?>();
            this.engine = engine;

            EmptyTexture = CreateTexture(new uint[] { 0xFFFFFFFF }, 1, 1);
        }

        internal ITexture? this[TextureHandle handle]
        {
            get => handle.Handle == 0 ? null : allTextures[handle.Handle - 1];
            set => allTextures[handle.Handle - 1] = value;
        }
        
        public void Dispose()
        {
            DisposeTexture(EmptyTexture);
            foreach (var tex in allTextures)
            {
                if (tex == null)
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

        private TextureHandle AddTexture(ITexture texture)
        {
            var textureHandle = new TextureHandle(allTextures.Count + 1);
            allTextures.Add(texture);
#if DEBUG_CREATE_CALLSTACK
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
            tex.Dispose();
#if DEBUG_CREATE_CALLSTACK
            createCallStack.Remove(tex);
#endif
            this[handle] = null;
        }
        
        public void ReplaceHandles(TextureHandle old, TextureHandle @new)
        {
            var oldTexture = GetTextureByHandle(old);
            var newTexture = GetTextureByHandle(@new);
            this[old] = newTexture;
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
            if (!image.TryGetSinglePixelSpan(out var span))
                throw new Exception("Cannot load texture " + path);
                
            var textureHandle = CreateTexture(new Rgba32[][]{span.ToArray()}, image.Width, image.Height, true);
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

        public TextureHandle CreateTexture(uint[] pixels, int width, int height)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateTexture(Rgba32[][] pixels, int width, int height, bool generateMips)
        {
            var texture = engine.Device.CreateTexture(width, height, pixels, generateMips);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateTextureArray(Rgba32[][][] textures, int width, int height)
        {
            var texture = engine.Device.CreateTextureArray(width, height, textures);
            return AddTexture(texture);
        }
        
        public TextureHandle CreateRenderTexture(int width, int height)
        {
            var texture = engine.Device.CreateRenderTexture(width, height);
            return AddTexture(texture);
        }

        public TextureHandle CreateDummyHandle()
        {
            return CreateTexture(new Rgba32[][] { new Rgba32[] { new Rgba32(255, 255, 255, 255) } }, 1, 1, false);
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
            
            srcTex!.ActivateSourceFrameBuffer();
            dstTex!.ActivateRenderFrameBuffer();
            engine.Device.device.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1,  dstX0, dstY0,  dstX1,  dstY1, mask, filter);
        }
        
        public void BlitRenderTextures(TextureHandle src, TextureHandle dst)
        {
            var srcTex = GetTextureByHandle(src) as RenderTexture;
            var dstTex = GetTextureByHandle(dst) as RenderTexture;
            
            srcTex!.ActivateSourceFrameBuffer();
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
