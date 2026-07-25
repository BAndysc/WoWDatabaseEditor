namespace TheEngine.Handles
{
    public struct ShaderHandle
    {
        internal int Handle { get; }

        internal ShaderHandle(int id)
        {
            Handle = id;
        }
    }

    public struct PipelineHandle : IEquatable<PipelineHandle>
    {
        private int handle;
        internal int Handle => (handle & 0xFFFFFF) - 1;

        internal int SortKey => handle; // encoded pipeline handle and shader handle

        // Decoded fields used to build MeshRenderer.SortKey. ShaderId is the top 8 bits;
        // PipelineId is the raw pipeline index (== Handle), -1 when empty.
        internal int ShaderId => (handle >> 24) & 0xFF;
        internal int PipelineId => Handle;

        // PipelineHandle has encoded shader handle in top 8 bits for fast sorting
        internal PipelineHandle(int id, int shaderHandle)
        {
            handle = (id + 1) | (shaderHandle << 24);
        }

        public bool IsEmpty => handle == 0;

        public static PipelineHandle Empty => default;

        public bool Equals(PipelineHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is PipelineHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return handle;
        }

        public static bool operator ==(PipelineHandle left, PipelineHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PipelineHandle left, PipelineHandle right)
        {
            return !left.Equals(right);
        }
    }
}
