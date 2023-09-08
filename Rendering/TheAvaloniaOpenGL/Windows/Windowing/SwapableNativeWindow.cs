namespace SPB.Windowing
{
    public abstract class SwappableNativeWindowBase : NativeWindowBase
    {
        public abstract uint SwapInterval { get; set; }

        public abstract void SwapBuffers();
    }
}
