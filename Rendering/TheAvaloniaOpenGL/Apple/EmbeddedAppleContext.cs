using TheAvaloniaOpenGL.Interfaces;

namespace TheAvaloniaOpenGL.Apple;

public class EmbeddedAppleContext : IRenderingOpenGlContext
{
    private ObjectiveC.NSOpenGLPixelFormat pf;
    private ObjectiveC.NSOpenGLContext glc;

    public EmbeddedAppleContext()
    {
        uint[] attribs = new uint[]
        {
            AppleOpenGlConstants.NSOpenGLPFANoRecovery,
            AppleOpenGlConstants.NSOpenGLPFAAccelerated,
            AppleOpenGlConstants.NSOpenGLPFADoubleBuffer,
            AppleOpenGlConstants.NSOpenGLPFAColorSize, 24,
            AppleOpenGlConstants.NSOpenGLPFADepthSize, 24,
            AppleOpenGlConstants.NSOpenGLPFAOpenGLProfile,
            AppleOpenGlConstants.NSOpenGLProfileVersion4_1Core,
            0
        };
        pf = new ObjectiveC.NSOpenGLPixelFormat(attribs);
        glc = new ObjectiveC.NSOpenGLContext(pf, ObjectiveC.Object.Null)
        {
        };
    }

    public ObjectiveC.NSOpenGLContext Ctx => glc;
    public ObjectiveC.NSOpenGLPixelFormat PixelFormat => pf;

    public void Dispose()
    {
        glc.Dispose();
        pf.Dispose();
    }

    public void MakeCurrentContext(IRenderingWindow? window)
    {
        if (window == null)
            glc.ClearCurrentContext();
        else
            glc.MakeCurrentContext();
    }

    public void FlushBuffer()
    {
        glc.FlushBuffer();
    }
}