namespace TheAvaloniaOpenGL.Interfaces;

public interface IRenderingOpenGlContext : System.IDisposable
{
    void MakeCurrentContext(IRenderingWindow? window);
}

public interface IRenderingWindow : System.IDisposable
{
    IntPtr Pointer { get; }
    event Action<long, long, bool, bool>? OnPointerPressed;
    event Action<long, long, bool, bool>? OnPointerReleased;
    event Action<long, long, bool, bool>? OnPointerMoved;
    void SwapBuffers();
}