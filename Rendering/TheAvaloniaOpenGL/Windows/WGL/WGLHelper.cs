using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform.Exceptions;
using SPB.Platform.Win32;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static SPB.Platform.Win32.Win32;

namespace SPB.Platform.WGL
{
    [SupportedOSPlatform("windows")]
    public static class WGLHelper
    {
        private static bool _isInit = false;

        private static IntPtr _opengl32Handle;

        private static string[] Extensions;

        private delegate IntPtr wglGetExtensionsStringARB(IntPtr hdc);
        private delegate IntPtr wglGetExtensionsStringEXT();

        private static wglGetExtensionsStringEXT GetExtensionsStringEXT;
        private static wglGetExtensionsStringARB GetExtensionsStringARB;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        private delegate bool wglChoosePixelFormatARBDelegate(IntPtr hdc, int[] piAttribIList, float[] pfAttribFList, int nMaxFormats, int[] piFormats, out int nNumFormats);

        private static wglChoosePixelFormatARBDelegate ChoosePixelFormatArb;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        private delegate bool wglGetPixelFormatAttribivARB(IntPtr hdc, int iPixelFormat, int iLayerPlane, uint nAttributes, int[] piAttributes, int[] values);

        private static wglGetPixelFormatAttribivARB GetPixelFormatAttribivARB;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        private delegate IntPtr wglCreateContextAttribsARBDelegate(IntPtr hDC, IntPtr hShareContext, int[] attribList);

        private static wglCreateContextAttribsARBDelegate CreateContextAttribsArb;

        public delegate bool wglSwapIntervalEXTDelegate(int interval);

        public static wglSwapIntervalEXTDelegate SwapInterval { get; private set; }

        public delegate int wglGetSwapIntervalEXTDelegate();

        public static wglGetSwapIntervalEXTDelegate GetSwapInterval { get; private set; }


        private static string GetExtensionsString()
        {
            IntPtr stringPtr;

            if (GetExtensionsStringARB != null)
            {
                stringPtr = GetExtensionsStringARB(WGL.GetCurrentDC());
            }
            else
            {
                stringPtr = GetExtensionsStringEXT();
            }

            return Marshal.PtrToStringAnsi(stringPtr);
        }

        public static IntPtr GetProcAddress(string procName)
        {
            EnsureInit();
            IntPtr result = WGL.GetProcAddress(procName);

            if (result == IntPtr.Zero)
            {
                NativeLibrary.TryGetExport(_opengl32Handle, procName, out result);
            }

            return result;
        }

        private static void EnsureExtensionPresence(string extensionName)
        {
            if (!Extensions.Contains(extensionName))
            {
                throw new PlatformException($"Missing needed WGL extension: \"{extensionName}\"");
            }
        }

        private static void EnsureInit()
        {
            if (!_isInit)
            {
                // Because WGL is terrible, we need to first create a context that will allow us to check extensions and query functions to create our contexes.

                IntPtr dummyWindow = Win32Helper.CreateNativeWindow(WindowStylesEx.WS_EX_OVERLAPPEDWINDOW,
                               WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_CLIPCHILDREN,
                               "SPB intermediary context",
                               0, 0, 1, 1);

                if (dummyWindow == IntPtr.Zero)
                {
                    throw new PlatformException($"CreateWindowEx failed: {Marshal.GetLastWin32Error()}");
                }

                // Enforce hidden (this is a hack around possible ShowWindow being ignored when started with a STARTUPINFO)
                ShowWindow(dummyWindow, ShowWindowFlag.SW_HIDE);

                IntPtr dummyWindowDC = GetDC(dummyWindow);

                PixelFormatDescriptor pfd = PixelFormatDescriptor.Create();

                pfd.Flags = PixelFormatDescriptorFlags.PFD_DRAW_TO_WINDOW |
                            PixelFormatDescriptorFlags.PFD_SUPPORT_OPENGL |
                            PixelFormatDescriptorFlags.PFD_DOUBLEBUFFER;
                pfd.PixelType = PixelType.PFD_TYPE_RGBA;
                pfd.ColorBits = 24;

                int res = SetPixelFormat(dummyWindowDC, ChoosePixelFormat(dummyWindowDC, ref pfd), ref pfd);

                if (res == 0)
                {
                    throw new PlatformException($"SetPixelFormat failed for dummy context: {Marshal.GetLastWin32Error()}");
                }

                IntPtr dummyContext = WGL.CreateContext(dummyWindowDC);

                if (dummyContext == IntPtr.Zero)
                {
                    throw new PlatformException($"WGL.CreateContext failed for dummy context: {Marshal.GetLastWin32Error()}");
                }

                IntPtr oldDC = WGL.GetCurrentDC();
                IntPtr oldContext = WGL.GetCurrentContext();

                if (!WGL.MakeCurrent(dummyWindowDC, dummyContext))
                {
                    // Ensure the previous context is restored and free the new context.
                    WGL.MakeCurrent(oldDC, oldContext);
                    WGL.DeleteContext(dummyContext);

                    throw new PlatformException($"WGL.MakeCurrent failed for dummy context: {Marshal.GetLastWin32Error()}");
                }

                GetExtensionsStringARB = Marshal.GetDelegateForFunctionPointer<wglGetExtensionsStringARB>(WGL.GetProcAddress("wglGetExtensionsStringARB"));
                GetExtensionsStringEXT = Marshal.GetDelegateForFunctionPointer<wglGetExtensionsStringEXT>(WGL.GetProcAddress("wglGetExtensionsStringEXT"));

                Extensions = GetExtensionsString().Split(" ");

                // Ensure that all extensions that we are requiring are present.
                EnsureExtensionPresence("WGL_ARB_create_context");
                EnsureExtensionPresence("WGL_ARB_create_context_profile");
                EnsureExtensionPresence("WGL_ARB_pixel_format"); // TODO: do not enforce this extension as we can use legacy PFDs too.
                EnsureExtensionPresence("WGL_EXT_swap_control");

                CreateContextAttribsArb = Marshal.GetDelegateForFunctionPointer<wglCreateContextAttribsARBDelegate>(WGL.GetProcAddress("wglCreateContextAttribsARB"));
                ChoosePixelFormatArb = Marshal.GetDelegateForFunctionPointer<wglChoosePixelFormatARBDelegate>(WGL.GetProcAddress("wglChoosePixelFormatARB"));
                GetPixelFormatAttribivARB = Marshal.GetDelegateForFunctionPointer<wglGetPixelFormatAttribivARB>(WGL.GetProcAddress("wglGetPixelFormatAttribivARB"));
                SwapInterval = Marshal.GetDelegateForFunctionPointer<wglSwapIntervalEXTDelegate>(WGL.GetProcAddress("wglSwapIntervalEXT"));
                GetSwapInterval = Marshal.GetDelegateForFunctionPointer<wglGetSwapIntervalEXTDelegate>(WGL.GetProcAddress("wglGetSwapIntervalEXT"));


                // We got everything we needed, clean up!
                WGL.MakeCurrent(oldDC, oldContext);
                WGL.DeleteContext(dummyContext);

                ReleaseDC(dummyWindow, dummyWindowDC);
                DestroyWindow(dummyWindow);

                _opengl32Handle = NativeLibrary.Load("opengl32.dll");

                _isInit = true;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AttributeInformation
        {
            public int SupportOpenGL;
            public int DrawToWindow;
            public int PixelType;
            public int Acceleration;
            public int RedBits;
            public int GreenBits;
            public int BlueBits;
            public int AlphaBits;
            public int DepthBits;
            public int StencilBits;
            public int AccumRedBits;
            public int AccumGreenBits;
            public int AccumBlueBits;
            public int AccumAlphaBits;
            public int Stereo;
            public int DoubleBuffer;
            public int Samples;

            public void CompareWithDesiredFormat(FramebufferFormat desiredFormat, out uint miss, out uint rawColorDifference, out uint extraDifference)
            {
                miss = 0;

                if (desiredFormat.StencilBits > 0 && StencilBits == 0)
                {
                    miss++;
                }

                if (desiredFormat.DepthBits > 0 && DepthBits == 0)
                {
                    miss++;
                }

                if (desiredFormat.Color.Alpha > 0 && AlphaBits == 0)
                {
                    miss++;
                }

                if (desiredFormat.Samples > 0 && Samples == 0)
                {
                    miss++;
                }

                rawColorDifference = 0;

                if (desiredFormat.Color.BitsPerPixel > 0)
                {
                    rawColorDifference = (uint)((desiredFormat.Color.Red - RedBits) * (desiredFormat.Color.Red - RedBits)
                                + (desiredFormat.Color.Blue - BlueBits) * (desiredFormat.Color.Blue - BlueBits)
                                + (desiredFormat.Color.Green - GreenBits) * (desiredFormat.Color.Green - GreenBits));
                }

                extraDifference = 0;

                if (desiredFormat.StencilBits > 0)
                {
                    extraDifference += (uint)((desiredFormat.StencilBits - StencilBits) * (desiredFormat.StencilBits - StencilBits));
                }

                if (desiredFormat.DepthBits > 0)
                {
                    extraDifference += (uint)((desiredFormat.DepthBits - DepthBits) * (desiredFormat.DepthBits - DepthBits));
                }

                if (desiredFormat.Accumulator.BitsPerPixel > 0)
                {
                    extraDifference += (uint)((desiredFormat.Accumulator.Red - AccumRedBits) * (desiredFormat.Accumulator.Red - AccumRedBits)
                                + (desiredFormat.Accumulator.Blue - AccumBlueBits) * (desiredFormat.Accumulator.Blue - AccumBlueBits)
                                + (desiredFormat.Accumulator.Green - AccumGreenBits) * (desiredFormat.Accumulator.Green - AccumGreenBits));
                }

                if (desiredFormat.Accumulator.Alpha > 0)
                {
                    extraDifference += (uint)((desiredFormat.Accumulator.Alpha - AccumAlphaBits) * (desiredFormat.Accumulator.Alpha - AccumAlphaBits));
                }

                if (desiredFormat.Samples > 0)
                {
                    extraDifference += (uint)(((int)desiredFormat.Samples - Samples) * ((int)desiredFormat.Samples - Samples));
                }
            }
        }

        private static int FindPerfectFormat(IntPtr dcHandle, FramebufferFormat format)
        {
            List<int> attributes = FramebufferFormatToPixelFormatAttributes(format);

            int[] formats = new int[1];

            if (!ChoosePixelFormatArb(dcHandle, attributes.ToArray(), null, formats.Length, formats, out int numFormat))
            {
                throw new PlatformException($"wglChoosePixelFormatARB failed: {Marshal.GetLastWin32Error()}");
            }

            if (numFormat == 0)
            {
                return -1;
            }

            return formats[0];
        }

        private static int FindClosestFormat(IntPtr dcHandle, FramebufferFormat format)
        {
            const int WGL_NO_ACCELERATION_ARB = 0x2025;

            int[] formatValue = new int[1];

            if (!GetPixelFormatAttribivARB(dcHandle, 1, 0, 1, new int[1] { (int)WGL.ARB.Attribute.WGL_NUMBER_PIXEL_FORMATS_ARB }, formatValue))
            {
                throw new PlatformException($"wglGetPixelFormatAttribivARB failed: {Marshal.GetLastWin32Error()}");
            }

            int formatCount = formatValue[0];

            List<int> tempAttributeList = new List<int>();

            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_SUPPORT_OPENGL_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_DRAW_TO_WINDOW_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_PIXEL_TYPE_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_ACCELERATION_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_RED_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_GREEN_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_BLUE_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_ALPHA_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_DEPTH_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_STENCIL_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_ACCUM_RED_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_ACCUM_GREEN_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_ACCUM_BLUE_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_ACCUM_ALPHA_BITS_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_STEREO_ARB);
            tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_DOUBLE_BUFFER_ARB);

            if (Extensions.Contains("WGL_ARB_multisample"))
            {
                tempAttributeList.Add((int)WGL.ARB.Attribute.WGL_SAMPLES_ARB);
            }

            int[] attributes = tempAttributeList.ToArray();

            int[] values = new int[Unsafe.SizeOf<AttributeInformation>() / sizeof(int)];

            int closestIndex = int.MaxValue;
            uint leastMissing = uint.MaxValue;
            uint leastRawColorDifference = uint.MaxValue;
            uint leastExtraDifference = uint.MaxValue;

            for (int index = 0; index < formatCount; index++)
            {
                if (!GetPixelFormatAttribivARB(dcHandle, index + 1, 0, (uint)attributes.Length, attributes, values))
                {
                    throw new PlatformException($"wglGetPixelFormatAttribivARB failed: {Marshal.GetLastWin32Error()}");
                }

                AttributeInformation information = MemoryMarshal.Cast<int, AttributeInformation>(values)[0];

                if (information.SupportOpenGL != 0 && information.Acceleration != WGL_NO_ACCELERATION_ARB)
                {
                    if (format.Stereo && information.Stereo == 0)
                    {
                        continue;
                    }

                    if (format.Buffers > 1 && information.DoubleBuffer == 0)
                    {
                        continue;
                    }

                    // Based on glfw3 algorithm.
                    information.CompareWithDesiredFormat(format, out uint missing, out uint rawColorDifference, out uint extraDifference);

                    if (missing < leastMissing)
                    {
                        closestIndex = index;
                    }
                    else if (missing == leastMissing)
                    {
                        if ((rawColorDifference < leastRawColorDifference) || ((rawColorDifference == leastRawColorDifference) && (extraDifference < leastExtraDifference)))
                        {
                            closestIndex = index;
                        }
                    }


                    if (closestIndex == index)
                    {
                        leastMissing = missing;
                        leastRawColorDifference = rawColorDifference;
                        leastExtraDifference = extraDifference;
                    }
                }

            }

            if (closestIndex == int.MaxValue)
            {
                closestIndex = -1;
            }

            return closestIndex;
        }

        private static List<int> FramebufferFormatToPixelFormatAttributes(FramebufferFormat format)
        {
            const int WGL_FULL_ACCELERATION_ARB = 0x2027;
            List<int> result = new List<int>();

            // Full acceleration required
            result.Add((int)WGL.ARB.Attribute.WGL_ACCELERATION_ARB);
            result.Add(WGL_FULL_ACCELERATION_ARB);

            // We use OpenGL so we need it...
            result.Add((int)WGL.ARB.Attribute.WGL_SUPPORT_OPENGL_ARB);
            result.Add(1);

            result.Add((int)WGL.ARB.Attribute.WGL_DRAW_TO_WINDOW_ARB);
            result.Add(1);

            if (format.Color.BitsPerPixel > 0)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_RED_BITS_ARB);
                result.Add(format.Color.Red);

                result.Add((int)WGL.ARB.Attribute.WGL_GREEN_BITS_ARB);
                result.Add(format.Color.Green);

                result.Add((int)WGL.ARB.Attribute.WGL_BLUE_BITS_ARB);
                result.Add(format.Color.Blue);

                result.Add((int)WGL.ARB.Attribute.WGL_ALPHA_BITS_ARB);
                result.Add(format.Color.Alpha);
            }

            if (format.DepthBits > 0)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_DEPTH_BITS_ARB);
                result.Add(format.DepthBits);
            }


            if (format.Buffers > 1)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_DOUBLE_BUFFER_ARB);
                result.Add(1);
            }

            if (format.StencilBits > 0)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_STENCIL_BITS_ARB);
                result.Add(format.StencilBits);
            }

            if (format.Accumulator.BitsPerPixel > 0)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_ACCUM_ALPHA_BITS_ARB);
                result.Add(format.Accumulator.Alpha);

                result.Add((int)WGL.ARB.Attribute.WGL_ACCUM_BLUE_BITS_ARB);
                result.Add(format.Accumulator.Blue);

                result.Add((int)WGL.ARB.Attribute.WGL_ACCUM_GREEN_BITS_ARB);
                result.Add(format.Accumulator.Green);

                result.Add((int)WGL.ARB.Attribute.WGL_ACCUM_RED_BITS_ARB);
                result.Add(format.Accumulator.Red);
            }

            if (format.Samples > 0)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_DOUBLE_BUFFER_ARB);
                result.Add(1);
            }

            if (format.Stereo)
            {
                result.Add((int)WGL.ARB.Attribute.WGL_STEREO_ARB);
                result.Add(format.Stereo ? 1 : 0);
            }

            // NOTE: Format is key: value, nothing in the spec specify if the end marker follow or not this format.
            // BODY: As such, we add an extra 0 just to be sure we don't break anything.
            result.Add(0);
            result.Add(0);

            return result;
        }

        private static List<int> GetContextCreationARBAttribute(int major, int minor, OpenGLContextFlags flags)
        {
            List<int> result = new List<int>();

            result.Add((int)WGL.ARB.CreateContext.MAJOR_VERSION);
            result.Add(major);

            result.Add((int)WGL.ARB.CreateContext.MINOR_VERSION);
            result.Add(minor);

            if (flags != 0)
            {
                if (flags.HasFlag(OpenGLContextFlags.Debug))
                {
                    result.Add((int)WGL.ARB.CreateContext.FLAGS);
                    result.Add((int)WGL.ARB.ContextFlags.DEBUG_BIT);
                }


                result.Add((int)WGL.ARB.CreateContext.PROFILE_MASK);

                if (flags.HasFlag(OpenGLContextFlags.Compat))
                {
                    result.Add((int)WGL.ARB.ContextProfileFlags.COMPATIBILITY_PROFILE);
                }
                else
                {
                    result.Add((int)WGL.ARB.ContextProfileFlags.CORE_PROFILE);
                }
            }

            // NOTE: Format is key: value, nothing in the spec specify if the end marker follow or not this format.
            // BODY: As such, we add an extra 0 just to be sure we don't break anything.
            result.Add(0);
            result.Add(0);

            return result;
        }

        public static void SetPerfectFormat(IntPtr dcHandle, IntPtr windowHandle, FramebufferFormat framebufferFormat)
        {
            bool hasDcHandle = dcHandle != IntPtr.Zero;
            if (!hasDcHandle) 
                dcHandle = GetDC(windowHandle);
            
            int pixelFormat = FindPerfectFormat(dcHandle, framebufferFormat);

            // Perfect match not availaible, search for the closest
            if (pixelFormat == -1)
            {
                pixelFormat = FindClosestFormat(dcHandle, framebufferFormat);
            }

            if (pixelFormat == -1)
            {
                throw new PlatformException("Cannot find a valid pixel format");
            }

            PixelFormatDescriptor pfd = PixelFormatDescriptor.Create();

            int res = DescribePixelFormat(dcHandle, pixelFormat, Marshal.SizeOf<PixelFormatDescriptor>(), ref pfd);

            if (res == 0)
            {
                throw new PlatformException($"DescribePixelFormat failed: {Marshal.GetLastWin32Error()}");
            }

            res = SetPixelFormat(dcHandle, pixelFormat, ref pfd);

            if (res == 0)
            {
                throw new PlatformException($"DescribePixelFormat failed: {Marshal.GetLastWin32Error()}");
            }
            
            if (!hasDcHandle)
                ReleaseDC(windowHandle, dcHandle);
        }
        
        public static IntPtr CreateContext(ref IntPtr windowHandle, FramebufferFormat framebufferFormat, int major, int minor, OpenGLContextFlags flags, bool directRendering, IntPtr shareContext)
        {
            EnsureInit();

            bool hasTempWindow = windowHandle == IntPtr.Zero;

            if (hasTempWindow)
            {
                windowHandle = Win32Helper.CreateNativeWindow(WindowStylesEx.WS_EX_OVERLAPPEDWINDOW,
                               WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_CLIPCHILDREN,
                               "SPB intermediary context",
                               0, 0, 1, 1);
            }

            IntPtr dcHandle = GetDC(windowHandle);

            SetPerfectFormat(dcHandle,  windowHandle, framebufferFormat);

            List<int> contextAttributes = GetContextCreationARBAttribute(major, minor, flags);

            IntPtr context = CreateContextAttribsArb(dcHandle, shareContext, contextAttributes.ToArray());

            ReleaseDC(windowHandle, dcHandle);

            if (hasTempWindow)
            {
                DestroyWindow(windowHandle);
            }

            return context;
        }
    }
}
