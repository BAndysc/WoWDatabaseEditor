using System;
using System.Runtime.InteropServices;

namespace TheEngine.Handles
{
    /// <summary>
    /// A bindless texture slot index, as stored inside GPU material data. It is binary-compatible
    /// with a plain <see cref="int"/> (single sequential int field, 4 bytes), so it can be used in
    /// place of an int field in a std140/std430 material struct without changing the byte layout the
    /// shader reads. The point is purely type information: an <see cref="int"/> field could be
    /// anything, but a <see cref="BindlessTextureId"/> field tells the material inspector (and any
    /// reader) that this slot holds a bindless texture, so it can preview the texture instead of a
    /// meaningless number. Obtain one from <c>TextureManager.GetBindlessIndex(texture)</c> (the int
    /// result converts implicitly).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BindlessTextureId : IEquatable<BindlessTextureId>
    {
        public readonly int Index;

        public BindlessTextureId(int index) => Index = index;

        public static implicit operator int(BindlessTextureId id) => id.Index;
        public static implicit operator BindlessTextureId(int index) => new(index);

        public bool Equals(BindlessTextureId other) => Index == other.Index;
        public override bool Equals(object? obj) => obj is BindlessTextureId other && Equals(other);
        public override int GetHashCode() => Index;
        public static bool operator ==(BindlessTextureId left, BindlessTextureId right) => left.Index == right.Index;
        public static bool operator !=(BindlessTextureId left, BindlessTextureId right) => left.Index != right.Index;

        public override string ToString() => $"BindlessTextureId({Index})";
    }
}
