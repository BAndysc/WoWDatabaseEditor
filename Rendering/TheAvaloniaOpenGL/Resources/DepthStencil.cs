using OpenGLBindings;

namespace TheAvaloniaOpenGL.Resources
{
    public class DepthStencil : IDisposable
    {
        private readonly IDevice device;
        private readonly bool zWrite;

        internal DepthStencil(IDevice device, bool zWrite)
        {
            this.device = device;
            this.zWrite = zWrite;
        }

        public void Activate()
        {
            if (zWrite)
                device.Enable(EnableCap.DepthTest);
            else
                device.Disable(EnableCap.DepthTest);
        }

        public void Dispose()
        {
        }
    }
}
