using SPB.Windowing;

namespace SPB.Graphics.OpenGL
{
    public abstract class OpenGLContextBase : IBaseContext
    {
        public FramebufferFormat FramebufferFormat { get; }
        public int Major { get; }
        public int Minor { get; }
        public OpenGLContextFlags Flags { get; }
        public bool DirectRendering { get; protected set; }
        public IntPtr ContextHandle { get; protected set; }
        public OpenGLContextBase? ShareContext { get; }

        public bool IsValid => ContextHandle != IntPtr.Zero;

        public bool IsDisposed { get; protected set; }

        public abstract bool IsCurrent { get; }

        public OpenGLContextBase(FramebufferFormat framebufferFormat, int major, int minor, OpenGLContextFlags flags, bool directRendering, OpenGLContextBase shareContext)
        {
            FramebufferFormat = framebufferFormat;
            Major = major;
            Minor = minor;
            Flags = flags;
            DirectRendering = directRendering;
            ShareContext = shareContext;
        }

        public abstract void Initialize(NativeWindowBase? window = null);

        public abstract void MakeCurrent(NativeWindowBase? window);

        public abstract IntPtr GetProcAddress(string procName);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}