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

    public sealed class NativeBuffer<T> : INativeBuffer where T : unmanaged
    {
        private static bool UseStorageBuffer = false;
        
        private readonly IDevice device;
        private readonly BufferInternalFormat internalFormat;

        public BufferTypeEnum BufferType { get; }

        public int Length { get; private set; }

        internal int BufferHandle { get; private set; }
        
        internal int TextureBufferHandle { get; private set; }
        
        internal BufferTarget GlBufferType { get; }
        
        internal NativeBuffer(IDevice device, BufferTypeEnum bufferType, int length, BufferInternalFormat internalFormat)
        {
            this.device = device;
            this.internalFormat = internalFormat;
            this.BufferType = bufferType;
            GlBufferType = BufferTypeToBindFlags(bufferType);
            CreateBuffer();
        }

        internal NativeBuffer(IDevice device, BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat internalFormat)
        {
            this.device = device;
            this.internalFormat = internalFormat;
            this.BufferType = bufferType;
            GlBufferType = BufferTypeToBindFlags(bufferType);
            CreateBufferWithData(data);
        }

        ~NativeBuffer()
        {
            if (BufferHandle != -1)
            {
                Console.WriteLine("Native buffer leaked!");
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
                device.BufferData(GlBufferType, new IntPtr(data.Length * Utilities.SizeOf<T>()), new IntPtr(pdata), UsageHint);
            
            device.BindBuffer(GlBufferType, 0);
        }

        private BufferUsageHint UsageHint =>
            IsStructuredBuffer ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw;

        public unsafe void UpdateBuffer(Span<T> newData)
        {
            device.BindBuffer(GlBufferType, BufferHandle);
            if (true || Length < newData.Length)
            {
                fixed (void* pdata = newData)
                    device.BufferData(GlBufferType, new IntPtr(newData.Length * Utilities.SizeOf<T>()), new IntPtr(pdata), UsageHint);
                Length = newData.Length;
            }
            else
            {
                fixed (void* pdata = newData)
                    device.BufferSubData(GlBufferType, IntPtr.Zero, new IntPtr(newData.Length * Utilities.SizeOf<T>()), new IntPtr(pdata));
            }
        }
        
        public unsafe void UpdateBuffer(ref T newData)
        {
            device.BindBuffer(GlBufferType, BufferHandle);
            if (true || Length < 1)
            {
                fixed (void* pdata = &newData)
                    device.BufferData(GlBufferType, new IntPtr(Utilities.SizeOf<T>()), new IntPtr(pdata), UsageHint);
                Length = 1;
            }    
            else
                fixed (void* pdata = &newData)
                    device.BufferSubData(GlBufferType, IntPtr.Zero, new IntPtr(Utilities.SizeOf<T>()), new IntPtr(pdata));
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

        public void Activate(int slot)
        {
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

        public void Dispose()
        {
            if (IsUsingBufferTexture)
                device.DeleteTexture(TextureBufferHandle);
            device.DeleteBuffer(BufferHandle);
            BufferHandle = -1;
        }
    }
}
