using System;
using System.Text.Unicode;

namespace TheEngine.ECS
{
    public readonly struct Entity : IUtf8SpanFormattable, ISpanFormattable
    {
        public readonly uint Id;
        public readonly uint Version;

        public Entity(uint id, uint version)
        {
            Id = id;
            Version = version;
        }

        public bool IsEmpty() => Id == 0 && Version == 0;

        public static Entity Empty => default;

        public bool Equals(Entity other) => Id == other.Id && Version == other.Version;

        public override bool Equals(object? obj) => obj is Entity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, Version);

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);

        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

        public override string ToString()
        {
            return $"Entity[{Id}, {Version}]";
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            FormattableString formattable = $"Entity[{Id}, {Version}]";
            return formattable.ToString(formatProvider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return destination.TryWrite(provider, $"Entity[{Id}, {Version}]",
                out charsWritten);
        }

        public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return Utf8.TryWrite(destination, provider, $"Entity[{Id}, {Version}]",
                out bytesWritten);
        }
    }
}