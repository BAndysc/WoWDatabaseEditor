using System;
using OpenGLBindings;

namespace TheAvaloniaOpenGL.Resources
{
    public class RenderTexture : IDisposable, ITexture
    {
        private Texture underlyingTexture;

        private int handle;
        private int depthHandle;

        private readonly IDevice device;
        
        internal RenderTexture(IDevice device, int width, int height)
        {
            handle = device.GenFramebuffer();
            underlyingTexture = new Texture(device, (uint[])null, width, height);
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
            device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D,
                underlyingTexture.Handle, 0);
            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)

            depthHandle = device.GenRenderbuffer();
            device.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthHandle);
            device.RenderbufferStorage(RenderbufferTarget.Renderbuffer,  RenderbufferStorage.Depth24Stencil8, width, height);
            device.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthHandle);

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
        }

        public void SetWrapping(WrapMode mode)
        {
            underlyingTexture.SetWrapping(mode);
        }

        public void Clear(float r, float g, float b, float a)
        {
            device.ClearColor(r, g, b, a);
            device.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //device.ImmediateContext.ClearRenderTargetView(TargetView, new Color4(r, g, b, a));
        }

        public void ActivateFrameBuffer()
        {
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
            device.Viewport(0, 0, (int)Width, (int)Height);
        }

        public void Dispose()
        {
            device.DeleteFramebuffer(handle);
            device.DeleteRenderbuffer(depthHandle);
            //TargetView.Dispose();
            underlyingTexture.Dispose();
        }
    }
}
