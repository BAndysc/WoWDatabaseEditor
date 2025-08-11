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
        internal int Handle => handle - 1;

        internal PipelineHandle(int id)
        {
            handle = id + 1;
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
