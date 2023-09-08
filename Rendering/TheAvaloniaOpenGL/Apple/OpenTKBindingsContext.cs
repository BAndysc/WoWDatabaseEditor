using OpenTK;

// based on https://github.com/Ryujinx/Ryujinx

namespace TheAvaloniaOpenGL.Apple;

internal class OpenTKBindingsContext : IBindingsContext
{
    private readonly Func<string, IntPtr> _getProcAddress;

    public OpenTKBindingsContext(Func<string, IntPtr> getProcAddress)
    {
        _getProcAddress = getProcAddress;
    }

    public IntPtr GetProcAddress(string procName)
    {
        return _getProcAddress(procName);
    }
}