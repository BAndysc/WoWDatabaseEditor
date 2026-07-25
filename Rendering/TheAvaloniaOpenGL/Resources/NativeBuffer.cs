// #define DEBUG_CREATE_CALLSTACK
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenGLBindings;
using TheMaths;

namespace TheAvaloniaOpenGL.Resources
{
    public enum BufferTypeEnum
    {
        Vertex,
        Index,
        ConstPixel,
        ConstVertex,
        StructuredBuffer,
        StructuredBufferVertexOnly,
        StructuredBufferPixelOnly
    }

    public enum BufferInternalFormat
    {
        None,
        Float4,
        Byte4,
        Int4,
        UInt
    }

    public interface INativeBuffer : IDisposable
    {
        void Activate(int slot);
    }

    /// <summary>
    /// Backend-neutral typed GPU buffer. Updating while a frame is in flight has
    /// orphaning semantics: draws already recorded keep reading the previous contents,
    /// draws recorded afterwards read the new contents (on GL via glBufferData orphaning,
    /// on Vulkan via buffer versioning).
    /// </summary>
    public interface INativeBuffer<T> : INativeBuffer where T : unmanaged
    {
        BufferTypeEnum BufferType { get; }
        int Length { get; }
        void UpdateBuffer(ReadOnlySpan<T> newData);
        void UpdateBuffer(ref T newData);
    }

    public abstract class NativeBufferBase : INativeBuffer
    {
        public int BufferHandle { get; protected set; }
        public abstract void Dispose();
        public abstract void Activate(int slot);

        internal abstract void InternalDispose();
#if DEBUG_CREATE_CALLSTACK
        internal StackTrace AllocationStackTrace { get; } = new StackTrace(2, true);
        internal StackTrace? DeallocationStackTrace { get; set; }
        internal long FrameAllocated = IDevice.FrameCount;
        internal long FrameDisposed = -1;
#endif
    }

    public sealed class NativeBuffer<T> : NativeBufferBase, INativeBuffer<T> where T : unmanaged
    {
        private static bool UseStorageBuffer = false;
        
        private readonly IDevice device;
        private readonly BufferInternalFormat internalFormat;

        public BufferTypeEnum BufferType { get; }

        public int Length { get; private set; }

        internal int TextureBufferHandle { get; private set; }
        
        internal BufferTarget GlBufferType { get; }

        internal int SizeOfT;

        internal NativeBuffer(IDevice device, BufferTypeEnum bufferType, int length, BufferInternalFormat internalFormat)
        {
            this.device = device;
            this.internalFormat = internalFormat;
            this.BufferType = bufferType;
            GlBufferType = BufferTypeToBindFlags(bufferType);
            CreateBuffer();
            SizeOfT = Unsafe.SizeOf<T>();
        }

        internal NativeBuffer(IDevice device, BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat internalFormat)
        {
            this.device = device;
            this.internalFormat = internalFormat;
            this.BufferType = bufferType;
            GlBufferType = BufferTypeToBindFlags(bufferType);
            SizeOfT = Unsafe.SizeOf<T>();
            CreateBufferWithData(data);
        }

        ~NativeBuffer()
        {
            if (BufferHandle != -1)
            {
#if DEBUG_CREATE_CALLSTACK
                DeallocationStackTrace = new StackTrace(0, true);
                FrameDisposed = IDevice.FrameCount;
#endif
                device.AddToDispose(this);
            }
        }

        private bool IsStructuredBuffer => BufferType == BufferTypeEnum.StructuredBuffer || BufferType == BufferTypeEnum.StructuredBufferPixelOnly || BufferType == BufferTypeEnum.StructuredBufferVertexOnly;

        public bool IsUsingBufferTexture => IsStructuredBuffer && !UseStorageBuffer;

        private SizedInternalFormat ToInternalFormat(BufferInternalFormat format)
        {
            switch (format)
            {
                case BufferInternalFormat.Float4:
                    return SizedInternalFormat.Rgba32f;
                case BufferInternalFormat.Byte4:
                    return SizedInternalFormat.Rgba8i;
                case BufferInternalFormat.Int4:
                    return SizedInternalFormat.Rgba32i;
                case BufferInternalFormat.UInt:
                    return SizedInternalFormat.R32ui;
                case BufferInternalFormat.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
        
        private void CreateBuffer()
        {
            BufferHandle = device.GenBuffer();
            if (BufferHandle <= 0)
            {
                throw new Exception("Failed to create buffer");
            }
            device.BindBuffer(GlBufferType, BufferHandle);
            if (IsStructuredBuffer && internalFormat == BufferInternalFormat.None)
                throw new Exception("You need to specify internal format for TextureBuffer");
            if (IsUsingBufferTexture)
            {
                TextureBufferHandle = device.GenTexture();
                device.BindTexture(TextureTarget.TextureBuffer, TextureBufferHandle);
                device.TexBuffer(TextureBufferTarget.TextureBuffer,  ToInternalFormat(internalFormat), BufferHandle);
            }
        }

        private unsafe void CreateBufferWithData(ReadOnlySpan<T> data)
        {
            CreateBuffer();
            Length = data.Length;
            device.BindBuffer(GlBufferType, BufferHandle);
            fixed (void* pdata = data)
                device.BufferData(GlBufferType, new IntPtr(data.Length * SizeOfT), new IntPtr(pdata), UsageHint);
            
            device.BindBuffer(GlBufferType, 0);
            device.TotalBufferBytes += Length * SizeOfT;
        }

        private BufferUsageHint UsageHint =>
            IsStructuredBuffer ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw;

        public unsafe void UpdateBuffer(ReadOnlySpan<T> newData)
        {
            device.BindBuffer(GlBufferType, BufferHandle);
            var oldLength = Length;
            if (true || Length < newData.Length)
            {
                fixed (void* pdata = newData)
                    device.BufferData(GlBufferType, new IntPtr(newData.Length * SizeOfT), new IntPtr(pdata), UsageHint);
                Length = newData.Length;
            }
            else
            {
                fixed (void* pdata = newData)
                    device.BufferSubData(GlBufferType, IntPtr.Zero, new IntPtr(newData.Length * SizeOfT), new IntPtr(pdata));
            }
            device.TotalBufferBytes += (Length - oldLength) * SizeOfT;
        }
        
        public unsafe void UpdateBuffer(ref T newData)
        {
            device.BindBuffer(GlBufferType, BufferHandle);
            var oldLength = Length;
            if (true || Length < 1)
            {
                fixed (void* pdata = &newData)
                    device.BufferData(GlBufferType, SizeOfT, new IntPtr(pdata), UsageHint);
                Length = 1;
            }    
            else
                fixed (void* pdata = &newData)
                    device.BufferSubData(GlBufferType, IntPtr.Zero, SizeOfT, new IntPtr(pdata));
            device.TotalBufferBytes += (Length - oldLength) * SizeOfT;
        }

        private static BufferTarget BufferTypeToBindFlags(BufferTypeEnum bufferType)
        {
            switch (bufferType)
            {
                case BufferTypeEnum.StructuredBuffer:
                case BufferTypeEnum.StructuredBufferPixelOnly:
                case BufferTypeEnum.StructuredBufferVertexOnly:
                    if (!UseStorageBuffer)
                        return BufferTarget.TextureBuffer;
                    else
                    {
                        throw new Exception("Those buffer requires Open GL >= 4.3, but we are limited to 4.1 only :|");
                        //return BufferTarget.ShaderStorageBuffer;
                    }
                case BufferTypeEnum.Vertex:
                    return BufferTarget.ArrayBuffer;
                case BufferTypeEnum.Index:
                    return BufferTarget.ElementArrayBuffer;
                case BufferTypeEnum.ConstPixel:
                case BufferTypeEnum.ConstVertex:
                    return BufferTarget.UniformBuffer;
                default:
                    throw new Exception("Unsupported buffer type");
            }
        }

        public override void Activate(int slot)
        {
            if (BufferHandle <= -1)
            {
#if DEBUG_CREATE_CALLSTACK
                throw new Exception($"[now = {IDevice.FrameCount}] Trying to activate that has been disposed!!!1! Alloc here (frame {FrameAllocated}):\n" + AllocationStackTrace + $"\nDealloc here ({FrameDisposed}):\n" + DeallocationStackTrace);
#else
                throw new Exception("Trying to activate that has been disposed!!!1! (enable DEBUG_CREATE_CALLSTACK to get more info)");
#endif
            }
            if (BufferType == BufferTypeEnum.Vertex)
            {
                device.BindBuffer(BufferTarget.ArrayBuffer, BufferHandle);
            }
            else if (BufferType == BufferTypeEnum.Index)
            {
                device.BindBuffer(BufferTarget.ElementArrayBuffer, BufferHandle);
            }
            else if (BufferType == BufferTypeEnum.ConstPixel)
            {
                device.BindBufferBase(BufferRangeTarget.UniformBuffer, slot, BufferHandle);
            }
            else if (BufferType == BufferTypeEnum.ConstVertex)
            {
                device.BindBufferBase(BufferRangeTarget.UniformBuffer, slot, BufferHandle);
            }
            else if (IsStructuredBuffer)
            {
                if (IsUsingBufferTexture)
                {
                    device.ActiveTextureUnit(slot);
                    device.BindTexture(TextureTarget.TextureBuffer, TextureBufferHandle);
                }
                else
                    device.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, slot, BufferHandle);
            }
            else
            {
                throw new Exception("Unsupported buffer type");
            }
        }

        internal override void InternalDispose()
        {
            if (BufferHandle <= -1)
                return;
            if (IsUsingBufferTexture)
                device.DeleteTexture(TextureBufferHandle);
            device.DeleteBuffer(BufferHandle);
            device.TotalBufferBytes -= Length * SizeOfT;
            BufferHandle = -1;
        }

        public override void Dispose()
        {
            if (BufferHandle != -1)
            {
#if DEBUG_CREATE_CALLSTACK
                DeallocationStackTrace = new StackTrace(0, true);
                FrameDisposed = IDevice.FrameCount;
#endif
                device.AddToDispose(this);
            }
        }
    }
}
