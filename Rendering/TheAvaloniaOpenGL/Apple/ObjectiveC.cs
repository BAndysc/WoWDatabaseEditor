using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// based on https://github.com/Ryujinx/Ryujinx

namespace TheAvaloniaOpenGL.Apple;

    [SupportedOSPlatform("macos")]
    public static partial class ObjectiveC
    {
        private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";

        [LibraryImport(ObjCRuntime, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr sel_getUid(string name);

        [LibraryImport(ObjCRuntime, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr objc_getClass(string name);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector, byte value);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr value);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr value, IntPtr value2);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector, NSRect point);
        
        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector, NSRect point, IntPtr obj);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(IntPtr receiver, Selector selector, double value);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr param);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, string param);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool bool_objc_msgSend(IntPtr receiver, Selector selector, IntPtr param);
        
        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool bool_objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static partial uint uint_objc_msgSend(IntPtr receiver, Selector selector);

        public class NSOpenGLPixelFormat : System.IDisposable
        {
            private static Object nsOpenGLPixelFormatClass;
            
            static NSOpenGLPixelFormat()
            {
                nsOpenGLPixelFormatClass = new Object("NSOpenGLPixelFormat");
            }
            
            internal Object obj;
            
            private NSOpenGLPixelFormat()
            {
                obj = nsOpenGLPixelFormatClass.GetFromMessage("alloc");
            }

            public NSOpenGLPixelFormat(uint[] attribs) : this()
            {
                Init(attribs);
            }

            public static NSOpenGLPixelFormat Alloc()
            {
                return new NSOpenGLPixelFormat();
            }

            public unsafe void Init(uint[] attribs)
            {
                fixed (uint* ptr = attribs)
                    obj.SendMessage("initWithAttributes:", new IntPtr(ptr));
            }
            
            public void Dispose()
            {
                obj.SendMessage("release");
            }
        }
        
        public class NSOpenGLContext : System.IDisposable
        {
            private static Object nsOpenGLContextClass;
            
            static NSOpenGLContext()
            {
                nsOpenGLContextClass = new Object("NSOpenGLContext");
            }
            
            internal Object obj;
            
            private NSOpenGLContext()
            {
                obj = nsOpenGLContextClass.GetFromMessage("alloc");
            }

            public NSOpenGLContext(NSOpenGLPixelFormat pixelFormat, Object shareContext) : this()
            {
                Init(pixelFormat, shareContext);
            }

            public NSOpenGLView View
            {
                set => obj.SendMessage("setView:", value.obj);
            }

            public static NSOpenGLContext Alloc()
            {
                return new NSOpenGLContext();
            }
            
            public void Init(NSOpenGLPixelFormat pixelFormat, Object shareContext)
            {
                obj.SendMessage("initWithFormat:shareContext:", pixelFormat.obj, shareContext);
            }

            public void MakeCurrentContext()
            {
                obj.SendMessage("makeCurrentContext");
            }

            public void ClearCurrentContext()
            {
                nsOpenGLContextClass.SendMessage("clearCurrentContext");
            }
            
            public void FlushBuffer()
            {
                obj.SendMessage("flushBuffer");
            }

            public void Dispose()
            {
                obj.SendMessage("release");
            }
        }
        
        public class NSOpenGLView : System.IDisposable
        {
            private static Object nsOpenGlViewClass;

            static NSOpenGLView()
            {
                nsOpenGlViewClass = new Object("NSOpenGLView");
            }
            
            internal Object obj;
            
            private NSOpenGLView()
            {
                obj = nsOpenGlViewClass.GetFromMessage("alloc");
            }

            public NSOpenGLView(NSRect frame, NSOpenGLPixelFormat pixelFormat) : this()
            {
                Init(frame, pixelFormat);
            }
            
            public static NSOpenGLView Alloc()
            {
                return new NSOpenGLView();
            }

            public bool WantsBestResolutionOpenGLSurface
            {
                get
                {
                    return obj.GetBoolFromMessage("wantsBestResolutionOpenGLSurface");
                }
                set
                {
                    obj.SendMessage("setWantsBestResolutionOpenGLSurface:", value ? 1 : 0);
                }
            }

            public NSOpenGLContext OpenGlContext
            {
                set => obj.SendMessage("setOpenGLContext:", value.obj);
            }

            public IntPtr Pointer => obj.ObjPtr;

            public void Init(NSRect frame, NSOpenGLPixelFormat pixelFormat)
            {
                obj.SendMessage("initWithFrame:pixelFormat:", frame, pixelFormat.obj);
            }
            
            public void Dispose()
            {
                obj.SendMessage("release");
            }
        }
        
        public readonly struct Object
        {
            public readonly IntPtr ObjPtr;

            private Object(IntPtr pointer)
            {
                ObjPtr = pointer;
            }

            public Object(string name)
            {
                ObjPtr = objc_getClass(name);
            }

            public static Object Null => new(IntPtr.Zero);

            public void SendMessage(Selector selector)
            {
                objc_msgSend(ObjPtr, selector);
            }

            public void SendMessage(Selector selector, byte value)
            {
                objc_msgSend(ObjPtr, selector, value);
            }

            public void SendMessage(Selector selector, Object obj)
            {
                objc_msgSend(ObjPtr, selector, obj.ObjPtr);
            }

            public void SendMessage(Selector selector, IntPtr ptr)
            {
                objc_msgSend(ObjPtr, selector, ptr);
            }
            
            public void SendMessage(Selector selector, Object obj, Object obj2)
            {
                objc_msgSend(ObjPtr, selector, obj.ObjPtr, obj2.ObjPtr);
            }

            public void SendMessage(Selector selector, NSRect point)
            {
                objc_msgSend(ObjPtr, selector, point);
            }
            
            public void SendMessage(Selector selector, NSRect point, Object obj)
            {
                objc_msgSend(ObjPtr, selector, point, obj.ObjPtr);
            }

            public void SendMessage(Selector selector, double value)
            {
                objc_msgSend(ObjPtr, selector, value);
            }

            public Object GetFromMessage(Selector selector)
            {
                return new Object(IntPtr_objc_msgSend(ObjPtr, selector));
            }

            public Object GetFromMessage(Selector selector, Object obj)
            {
                return new Object(IntPtr_objc_msgSend(ObjPtr, selector, obj.ObjPtr));
            }

            public Object GetFromMessage(Selector selector, NSString nsString)
            {
                return new Object(IntPtr_objc_msgSend(ObjPtr, selector, nsString.StrPtr));
            }

            public Object GetFromMessage(Selector selector, string param)
            {
                return new Object(IntPtr_objc_msgSend(ObjPtr, selector, param));
            }

            public bool GetBoolFromMessage(Selector selector, Object obj)
            {
                return bool_objc_msgSend(ObjPtr, selector, obj.ObjPtr);
            }

            public bool GetBoolFromMessage(Selector selector)
            {
                return bool_objc_msgSend(ObjPtr, selector);
            }
            
            public uint GetUIntFromMessage(Selector selector)
            {
                return uint_objc_msgSend(ObjPtr, selector);
            }
        }

        public readonly struct Selector
        {
            public readonly IntPtr SelPtr;

            private Selector(string name)
            {
                SelPtr = sel_getUid(name);
            }

            public static implicit operator Selector(string value) => new(value);
        }

        public readonly struct NSString
        {
            public readonly IntPtr StrPtr;

            public NSString(string aString)
            {
                IntPtr nsString = objc_getClass("NSString");
                StrPtr = IntPtr_objc_msgSend(nsString, "stringWithUTF8String:", aString);
            }

            public static implicit operator IntPtr(NSString nsString) => nsString.StrPtr;
        }

        public readonly struct NSPoint
        {
            public readonly double X;
            public readonly double Y;

            public NSPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public readonly struct NSRect
        {
            public readonly NSPoint Pos;
            public readonly NSPoint Size;

            public NSRect(double x, double y, double width, double height)
            {
                Pos = new NSPoint(x, y);
                Size = new NSPoint(width, height);
            }
        }
    }