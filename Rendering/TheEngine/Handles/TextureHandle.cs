namespace TheEngine.Handles
{
    public struct TextureHandle
    {
        internal int Handle { get; }

        public bool Equals(TextureHandle other)
        {
            return Handle == other.Handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is TextureHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Handle;
        }

        public static bool operator ==(TextureHandle left, TextureHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextureHandle left, TextureHandle right)
        {
            return !left.Equals(right);
        }

        internal TextureHandle(int id)
        {
            Handle = id;
        }

        public override string ToString()
        {
            return $"TextureHandle[{Handle}]";
        }
    }
}
