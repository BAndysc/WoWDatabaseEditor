using System;

namespace WDE.Common.Debugging;

public readonly struct DebugPointId : IEquatable<DebugPointId>
{
    private readonly int index;
    private readonly int version;

    public int Index => index - 1;

    public int Version => version;

    public DebugPointId(int index, int version)
    {
        this.index = index + 1;
        this.version = version;
    }

    public static DebugPointId Empty => default;

    public bool Equals(DebugPointId other) => index == other.index && version == other.version;

    public override bool Equals(object? obj) => obj is DebugPointId other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(index.GetHashCode(), version.GetHashCode());

    public static bool operator ==(DebugPointId left, DebugPointId right) => left.Equals(right);

    public static bool operator !=(DebugPointId left, DebugPointId right) => !left.Equals(right);
}