using System;
using System.Runtime.CompilerServices;
using OpenGLBindings;

[assembly: InternalsVisibleTo("TheEngine")]
namespace TheAvaloniaOpenGL.Resources
{
    internal class RenderTexture : IDisposable, ITexture
    {
        private Texture underlyingTexture;
        private Texture[]? nextTextures;

        private int handle;
        private int depthHandle;

        private readonly IDevice device;
        
        internal RenderTexture(IDevice device, int width, int height, int colorAttachments = 1)
        {
            if (colorAttachments <= 0 || colorAttachments >= 5)
                throw new ArgumentOutOfRangeException(nameof(colorAttachments));
            handle = device.GenFramebuffer();
            underlyingTexture = new Texture(device, (uint[])null, width, height);
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
            device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, underlyingTexture.Handle, 0);
            
            if (colorAttachments > 1)
            {
                nextTextures = new Texture[colorAttachments - 1];
                for (int i = 0; i < nextTextures.Length; i++)
                {
                    nextTextures[i] = new Texture(device, (uint[])null, width, height, TextureFormat.R32ui);
                    device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i + 1, TextureTarget.Texture2D, nextTextures[i].Handle, 0);
                }
            }
            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)

            depthHandle = device.GenRenderbuffer();
            device.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthHandle);
            device.RenderbufferStorage(RenderbufferTarget.Renderbuffer,  RenderbufferStorage.DepthComponent, width, height);
            device.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthHandle);

            device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            this.device = device;
        }
        
        public int Width => underlyingTexture.Width;

        public int Height => underlyingTexture.Height;
        
        public void Activate(int slot)
        {
            underlyingTexture.Activate(slot);
        }

        public void SetFiltering(FilteringMode mode)
        {
            underlyingTexture.SetFiltering(mode);
            if (nextTextures != null)
                foreach (var t in nextTextures)
                    t.SetFiltering(mode);
        }

        public void SetWrapping(WrapMode mode)
        {
            underlyingTexture.SetWrapping(mode);
            if (nextTextures != null)
                foreach (var t in nextTextures)
                    t.SetWrapping(mode);
        }

        public void Clear(float r, float g, float b, float a)
        {
            device.ClearColor(r, g, b, a);
            device.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //device.ImmediateContext.ClearRenderTargetView(TargetView, new Color4(r, g, b, a));
        }

        public void ActivateFrameBuffer(float viewPortScale = 1)
        {
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
            device.Viewport(0, 0, (int)Math.Max(1, Width * viewPortScale), (int)Math.Max(1, Height * viewPortScale));
            Span<DrawBuffersEnum> buffers = stackalloc DrawBuffersEnum[nextTextures?.Length + 1 ?? 1];
            for (int i = 0; i < buffers.Length; i++)
                buffers[i] = DrawBuffersEnum.ColorAttachment0 + i;
            device.DrawBuffers(buffers);
        }
        
        public void ActivateSourceFrameBuffer(int attachment)
        {
            device.BindFramebuffer(FramebufferTarget.ReadFramebuffer, handle);
            device.ReadBuffer(ReadBufferMode.ColorAttachment0 + attachment);
        }
        
        public void ActivateRenderFrameBuffer()
        {
            device.BindFramebuffer(FramebufferTarget.DrawFramebuffer, handle);
        }

        public void Dispose()
        {
            device.DeleteFramebuffer(handle);
            device.DeleteRenderbuffer(depthHandle);
            //TargetView.Dispose();
            underlyingTexture.Dispose();
            if (nextTextures != null)
            {
                for (var index = 0; index < nextTextures.Length; index++)
                {
                    var texture = nextTextures[index];
                    texture.Dispose();
                }
            }
        }

        public void ActivateAttachment(int colorAttachmentIndex, int slot)
        {
            GetTexture(colorAttachmentIndex).Activate(slot);
        }

        public Texture GetTexture(int colorAttachmentIndex)
        {
            if (colorAttachmentIndex == 0)
                return underlyingTexture;
            return nextTextures[colorAttachmentIndex - 1];
        }
    }
}
