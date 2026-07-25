// #define DEBUG_CREATE_CALLSTACK
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TheMaths;

namespace TheEngine.Resources
{
    public enum BufferTypeEnum
    {
        Vertex,
        Index,
        ConstPixel,
        ConstVertex,
        StructuredBuffer,
        StructuredBufferVertexOnly,
        StructuredBufferPixelOnly,
        IndirectCommands
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
}
