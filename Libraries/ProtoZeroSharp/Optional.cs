namespace ProtoZeroSharp;

public struct Optional<T> where T : unmanaged
{
    public bool HasValue;
    public T Value;

    public static bool operator ==(Optional<T> a, Optional<T> b)
    {
        if (a.HasValue != b.HasValue)
            return false;
        if (!a.HasValue)
            return true;
        return a.Value.Equals(b.Value);
    }

    public override int GetHashCode()
    {
        return HasValue ? Value.GetHashCode() : 0;
    }

    public override bool Equals(object obj)
    {
        return obj is Optional<T> other && this == other || obj is T value && HasValue && Value.Equals(value);
    }

    public static bool operator !=(Optional<T> a, Optional<T> b) => !(a == b);
}