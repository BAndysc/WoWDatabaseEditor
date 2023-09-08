using SPB.Graphics;
using SPB.Graphics.Exceptions;
using SPB.Graphics.OpenGL;
using SPB.Platform.WGL;
using SPB.Platform.Win32;
using SPB.Windowing;
using System.Runtime.InteropServices;

namespace SPB.Platform
{
    public sealed class PlatformHelper
    {
        private static bool _isResolverRegistered;

        private static readonly Dictionary<string, List<string>> LibrariesMapping = new Dictionary<string, List<string>>()
        {
            ["glx"] = new List<string> { "libGL.so.1", "libGL.so" },

            // Required for Fedora/CentOS/RedHat
            ["libX11"] = new List<string> { "libX11.so.6", "libX11.so" },

            // Required for Fedora/CentOS/RedHat
            ["libX11-xcb"] = new List<string> { "libX11-xcb.so.1", "libX11-xcb.so" }
        };

        internal static void EnsureResolverRegistered()
        {
            if (!_isResolverRegistered)
            {
                NativeLibrary.SetDllImportResolver(typeof(PlatformHelper).Assembly, (name, assembly, path) =>
                {
                    if (LibrariesMapping.TryGetValue(name, out List<string> values))
                    {
                        foreach (string value in values)
                        {
                            if (NativeLibrary.TryLoad(value, assembly, path, out IntPtr handle))
                            {
                                return handle;
                            }
                        }
                    }

                    return IntPtr.Zero;
                });

                _isResolverRegistered = true;
            }

        }

        internal static bool IsLibraryAvailable(string name)
        {
            if (LibrariesMapping.TryGetValue(name, out List<string> values))
            {
                foreach (string value in values)
                {
                    if (NativeLibrary.TryLoad(value, out IntPtr handle))
                    {
                        NativeLibrary.Free(handle);

                        return true;
                    }
                }
            }
            else if (NativeLibrary.TryLoad(name, out IntPtr handle))
            {
                NativeLibrary.Free(handle);

                return true;
            }

            return false;
        }

        public static SwappableNativeWindowBase CreateOpenGLWindow(FramebufferFormat format, int x, int y, int width, int height)
        {
            if (OperatingSystem.IsLinux())
            {
                throw new Exception();
                // TODO: detect X11/Wayland/DRI
                //return X11Helper.CreateGLXWindow(new NativeHandle(X11.X11.DefaultDisplay), format, x, y, width, height);
            }
            else if (OperatingSystem.IsWindows())
            {
                // TODO pass format
                return Win32Helper.CreateWindowForWGL(x, y, width, height);
            }

            throw new NotImplementedException();
        }

        public static OpenGLContextBase CreateOpenGLContext(FramebufferFormat framebufferFormat, int major, int minor, OpenGLContextFlags flags = OpenGLContextFlags.Default, bool directRendering = true, OpenGLContextBase shareContext = null)
        {
            if (OperatingSystem.IsLinux())
            {
                throw new Exception();
                // // TODO: detect X11/Wayland/DRI
                // if (shareContext != null && !(shareContext is GLXOpenGLContext))
                // {
                //     throw new ContextException($"shared context must be of type {typeof(GLXOpenGLContext).Name}.");
                // }
                //
                // return new GLXOpenGLContext(framebufferFormat, major, minor, flags, directRendering, (GLXOpenGLContext)shareContext);
            }
            else if (OperatingSystem.IsWindows())
            {
                if (shareContext != null && !(shareContext is WGLOpenGLContext))
                {
                    throw new ContextException($"shared context must be of type {typeof(WGLOpenGLContext).Name}.");
                }

                return new WGLOpenGLContext(framebufferFormat, major, minor, flags, directRendering, (WGLOpenGLContext)shareContext);
            }

            throw new NotImplementedException();
        }
    }
}
