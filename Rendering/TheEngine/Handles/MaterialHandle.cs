namespace TheEngine.Handles
{
    public struct MaterialHandle
    {
        internal int Handle { get; }

        internal MaterialHandle(int id)
        {
            Handle = id;
        }

        public bool Equals(MaterialHandle other)
        {
            return Handle == other.Handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is MaterialHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Handle;
        }

        public static bool operator ==(MaterialHandle left, MaterialHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MaterialHandle left, MaterialHandle right)
        {
            return !left.Equals(right);
        }
    }
}
