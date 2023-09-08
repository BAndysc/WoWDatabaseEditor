using OpenTK;

namespace SPB.Graphics
{
    public interface IBaseContext : IBindingsContext, IDisposable
    {
        IntPtr ContextHandle { get; }
    }
}
