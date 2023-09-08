using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HDC = System.IntPtr;
using HGLRC = System.IntPtr;

namespace SPB.Platform.WGL
{
    [SupportedOSPlatform("windows")]
    internal sealed class WGL
    {
        private const string LibraryName = "OPENGL32.DLL";

        [DllImport(LibraryName, EntryPoint = "wglCreateContext", SetLastError = true)]
        public static extern HGLRC CreateContext(HDC hDc);

        [DllImport(LibraryName, EntryPoint = "wglDeleteContext", SetLastError = true)]
        public static extern bool DeleteContext(HGLRC context);

        [DllImport(LibraryName, EntryPoint = "wglMakeCurrent", SetLastError = true)]
        public extern static bool MakeCurrent(HDC hDc, HGLRC newContext);

        [DllImport(LibraryName, EntryPoint = "wglGetCurrentContext")]
        public extern static HGLRC GetCurrentContext();

        [DllImport(LibraryName, EntryPoint = "wglGetCurrentDC")]
        public extern static HDC GetCurrentDC();

        [DllImport(LibraryName, EntryPoint = "wglGetProcAddress", SetLastError = true, CharSet = CharSet.Ansi)]
        public extern static IntPtr GetProcAddress(string name);

        internal sealed class ARB
        {
            public enum Attribute : int
            {
                // WGL_ARB_pixel_format attributes
                WGL_NUMBER_PIXEL_FORMATS_ARB = 0x2000,
                WGL_DRAW_TO_WINDOW_ARB = 0x2001,
                WGL_DRAW_TO_BITMAP_ARB = 0x2002,
                WGL_ACCELERATION_ARB = 0x2003,
                WGL_NEED_PALETTE_ARB = 0x2004,
                WGL_NEED_SYSTEM_PALETTE_ARB = 0x2005,
                WGL_SWAP_LAYER_BUFFERS_ARB = 0x2006,
                WGL_SWAP_METHOD_ARB = 0x2007,
                WGL_NUMBER_OVERLAYS_ARB = 0x2008,
                WGL_NUMBER_UNDERLAYS_ARB = 0x2009,
                WGL_TRANSPARENT_ARB = 0x200A,
                WGL_TRANSPARENT_RED_VALUE_ARB = 0x2037,
                WGL_TRANSPARENT_GREEN_VALUE_ARB = 0x2038,
                WGL_TRANSPARENT_BLUE_VALUE_ARB = 0x2039,
                WGL_TRANSPARENT_ALPHA_VALUE_ARB = 0x203A,
                WGL_TRANSPARENT_INDEX_VALUE_ARB = 0x203B,
                WGL_SHARE_DEPTH_ARB = 0x200C,
                WGL_SHARE_STENCIL_ARB = 0x200D,
                WGL_SHARE_ACCUM_ARB = 0x200E,
                WGL_SUPPORT_GDI_ARB = 0x200F,
                WGL_SUPPORT_OPENGL_ARB = 0x2010,
                WGL_DOUBLE_BUFFER_ARB = 0x2011,
                WGL_STEREO_ARB = 0x2012,
                WGL_PIXEL_TYPE_ARB = 0x2013,
                WGL_COLOR_BITS_ARB = 0x2014,
                WGL_RED_BITS_ARB = 0x2015,
                WGL_RED_SHIFT_ARB = 0x2016,
                WGL_GREEN_BITS_ARB = 0x2017,
                WGL_GREEN_SHIFT_ARB = 0x2018,
                WGL_BLUE_BITS_ARB = 0x2019,
                WGL_BLUE_SHIFT_ARB = 0x201A,
                WGL_ALPHA_BITS_ARB = 0x201B,
                WGL_ALPHA_SHIFT_ARB = 0x201C,
                WGL_ACCUM_BITS_ARB = 0x201D,
                WGL_ACCUM_RED_BITS_ARB = 0x201E,
                WGL_ACCUM_GREEN_BITS_ARB = 0x201F,
                WGL_ACCUM_BLUE_BITS_ARB = 0x2020,
                WGL_ACCUM_ALPHA_BITS_ARB = 0x2021,
                WGL_DEPTH_BITS_ARB = 0x2022,
                WGL_STENCIL_BITS_ARB = 0x2023,
                WGL_AUX_BUFFERS_ARB = 0x2024,

                WGL_SAMPLES_ARB = 0x2042,
            }

            public enum CreateContext : int
            {
                MAJOR_VERSION = 0x2091,
                MINOR_VERSION = 0x2092,
                LAYER_PLANE = 0x2093,
                FLAGS = 0x2094,
                PROFILE_MASK = 0x9126,
            }

            public enum ContextFlags : int
            {
                DEBUG_BIT = 0x1,
                FORWARD_COMPATIBLE_BIT = 0x2,
            }

            public enum ContextProfileFlags : int
            {
                CORE_PROFILE = 0x1,
                COMPATIBILITY_PROFILE = 0x2,
            }
        }
    }
}
