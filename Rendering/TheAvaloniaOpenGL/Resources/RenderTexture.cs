using System.Runtime.CompilerServices;
using OpenGLBindings;

[assembly: InternalsVisibleTo("TheEngine")]
namespace TheAvaloniaOpenGL.Resources
{
    internal class RenderTexture : IDisposable, INativeTexture
    {
        private Texture2D? underlyingTexture;
        private bool ownsColorTexture;
        private Texture2D[]? nextTextures;

        private int handle;
        private int depthHandle = -1;

        private readonly IDevice device;
        
        internal RenderTexture(IDevice device, Texture2D colorAttachment, Texture2D depthTexture, Texture2D? colorAttachment1)
        {
            underlyingTexture = colorAttachment;
            handle = device.GenFramebuffer();
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);

            device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorAttachment.Handle, 0);
            device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture.Handle, 0);
            if (colorAttachment1 != null)
            {
                nextTextures = [colorAttachment1];
                device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, colorAttachment1.Handle, 0);
            }

            device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            this.device = device;
        }
        
        internal RenderTexture(IDevice device, int width, int height, int colorAttachments = 1, Texture2D? depthTexture = null)
        {
            if (colorAttachments < 0 || colorAttachments >= 5)
                throw new ArgumentOutOfRangeException(nameof(colorAttachments));
            handle = device.GenFramebuffer();
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
            ownsColorTexture = true;

            if (colorAttachments >= 1)
            {
                underlyingTexture = new Texture2D(device, (uint[])null, width, height);
                device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, underlyingTexture.Handle, 0);
            }

            if (colorAttachments > 1)
            {
                nextTextures = new Texture2D[colorAttachments - 1];
                for (int i = 0; i < nextTextures.Length; i++)
                {
                    nextTextures[i] = new Texture2D(device, (uint[])null, width, height, TextureFormat.R32ui);
                    device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i + 1, TextureTarget.Texture2D, nextTextures[i].Handle, 0);
                }
            }

            if (depthTexture == null)
            {
                // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
                depthHandle = device.GenRenderbuffer();
                device.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthHandle);
                device.RenderbufferStorage(RenderbufferTarget.Renderbuffer,  RenderbufferStorage.DepthComponent32f, width, height);
                device.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthHandle);
            }
            else
            {
                // create a depth texture that can be sampled
                //depthTexture = new Texture(device, (uint[])null, width, height, TextureFormat.DepthComponent);
                device.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture.Handle, 0);
            }

            device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            this.device = device;
        }
        
        public int Width => underlyingTexture.Width;

        public int Height => underlyingTexture.Height;

        public int NativeHandle => handle;

        public void Activate(int slot)
        {
            if (underlyingTexture == null)
                throw new Exception("Cannot activate Depth RenderTexture!");
            underlyingTexture.Activate(slot);
        }

        public void SetFiltering(FilteringMode mode)
        {
            underlyingTexture?.SetFiltering(mode);
            if (nextTextures != null)
                foreach (var t in nextTextures)
                    t.SetFiltering(mode);
        }

        public void SetWrapping(WrapMode mode)
        {
            underlyingTexture?.SetWrapping(mode);
            if (nextTextures != null)
                foreach (var t in nextTextures)
                    t.SetWrapping(mode);
        }

        public int SizeInBytes => 0;

        public void Clear(float r, float g, float b, float a)
        {
            device.ClearColor(r, g, b, a);
            if (underlyingTexture == null)
                device.Clear(ClearBufferMask.DepthBufferBit);
            else
                device.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void ActivateFrameBuffer(float viewPortScale = 1)
        {
            device.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
            device.Viewport(0, 0, (int)Math.Max(1, Width * viewPortScale), (int)Math.Max(1, Height * viewPortScale));
            Span<DrawBuffersEnum> buffers = stackalloc DrawBuffersEnum[(nextTextures?.Length ?? 0) + (underlyingTexture == null ? 0 : 1)];
            for (int i = 0; i < buffers.Length; i++)
                buffers[i] = DrawBuffersEnum.ColorAttachment0 + i;
            device.DrawBuffers(buffers);
        }
        
        public void ActivateSourceFrameBuffer(int attachment)
        {
            device.BindFramebuffer(FramebufferTarget.ReadFramebuffer, handle);
            if (underlyingTexture == null)
                device.ReadBuffer(ReadBufferMode.None);
            else
                device.ReadBuffer(ReadBufferMode.ColorAttachment0 + attachment);
        }
        
        public void ActivateRenderFrameBuffer()
        {
            device.BindFramebuffer(FramebufferTarget.DrawFramebuffer, handle);
        }

        public void Dispose()
        {
            if (handle != 0)
            {
                device.DeleteFramebuffer(handle);
                if (depthHandle != -1)
                    device.DeleteRenderbuffer(depthHandle);
                handle = 0;
                depthHandle = -1;
            }
            //TargetView.Dispose();
            if (ownsColorTexture)
            {
                underlyingTexture?.Dispose();
                if (nextTextures != null)
                {
                    for (var index = 0; index < nextTextures.Length; index++)
                    {
                        var texture = nextTextures[index];
                        texture.Dispose();
                    }
                }
            }
        }

        public void ActivateAttachment(int colorAttachmentIndex, int slot)
        {
            GetTexture(colorAttachmentIndex).Activate(slot);
        }

        public Texture2D GetTexture(int colorAttachmentIndex)
        {
            if (colorAttachmentIndex == 0)
                return underlyingTexture;
            return nextTextures[colorAttachmentIndex - 1];
        }
    }
}
