using System.Runtime.CompilerServices;
using System.Text;
using TheEngine.ECS;

namespace TheEngine.Components;

public struct EntityName : IComponentData
{
    public const int MaxLength = 30;
    private FixedString31 bytes;
    private byte actualLength; // without null terminator

    public static implicit operator EntityName(ReadOnlySpan<byte> array) => FromSpan(array);

    public static implicit operator EntityName(string str) => FromString(str);

    public unsafe ReadOnlySpan<byte> AsSpan()
    {
        fixed (FixedString31* ptr = &bytes)
        {
            return new Span<byte>(ptr, actualLength + 1);
        }
    }

    public static unsafe EntityName FromSpan(ReadOnlySpan<byte> str)
    {
        EntityName fixedString = default;
        var ptr = &fixedString.bytes;
        Span<byte> span = new Span<byte>(ptr, 31);
        fixedString.actualLength = (byte)Math.Min(str.Length, 30);
        str.Slice(0, fixedString.actualLength).CopyTo(span);
        fixedString.bytes[fixedString.actualLength] = 0;
        return fixedString;
    }

    public static unsafe EntityName FromString(string str)
    {
        EntityName fixedString = default;
        var ptr = &fixedString.bytes;
        Span<byte> span = new Span<byte>(ptr, 31);
        fixedString.actualLength = str == null ? (byte)0 : (byte)Encoding.UTF8.GetBytes(str.AsSpan().Slice(0, Math.Min(str.Length, 30)), span.Slice(0, 30));
        fixedString.bytes[fixedString.actualLength] = 0;
        return fixedString;
    }

    [InlineArray(31)]
    private struct FixedString31
    {
        private byte first;
    }
}