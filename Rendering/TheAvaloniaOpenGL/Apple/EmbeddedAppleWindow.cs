using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using TheAvaloniaOpenGL.Interfaces;

namespace TheAvaloniaOpenGL.Apple;

public class EmbeddedAppleWindow : IRenderingWindow
{
    private ObjectiveC.NSOpenGLView nsView;

    private EmbeddedAppleContext context;
    
    public IntPtr Pointer => nsView.Pointer;
    public event Action<long, long, bool, bool>? OnPointerPressed;
    public event Action<long, long, bool, bool>? OnPointerReleased;
    public event Action<long, long, bool, bool>? OnPointerMoved;
    
    [DllImport("libdl.dylib", EntryPoint = "NSIsSymbolNameDefined")]
    private static extern bool NSIsSymbolNameDefined(string s);
    
    [DllImport("libdl.dylib", EntryPoint = "NSLookupAndBindSymbol")]
    private static extern IntPtr NSLookupAndBindSymbol(string s);
    
    [DllImport("libdl.dylib", EntryPoint = "NSAddressOfSymbol")]
    private static extern IntPtr NSAddressOfSymbol(IntPtr symbol);

    private static IntPtr aglGetProcAddress(string s)
    {
        string fname = "_" + s;
        if (!NSIsSymbolNameDefined(fname))
            return IntPtr.Zero;

        IntPtr symbol = NSLookupAndBindSymbol(fname);
        if (symbol != IntPtr.Zero)
            symbol = NSAddressOfSymbol(symbol);

        return symbol;
    }
    
    public EmbeddedAppleWindow(EmbeddedAppleContext context)
    {
        this.context = context;
        nsView = new ObjectiveC.NSOpenGLView(new ObjectiveC.NSRect(0, 0, 100, 100), context.PixelFormat)
        {
            WantsBestResolutionOpenGLSurface = true
        };

        context.Ctx.View = nsView;

        nsView.OpenGlContext = context.Ctx;
        
        context.Ctx.MakeCurrentContext();
        GL.LoadBindings(new OpenTKBindingsContext(aglGetProcAddress));
        //context.Ctx.ClearCurrentContext();
    }

    public void SwapBuffers()
    {
        context.FlushBuffer();
    }

    public void Dispose()
    {
        nsView.Dispose();
    }
}