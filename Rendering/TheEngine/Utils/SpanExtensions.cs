namespace TheEngine.Utils;

public static class SpanExtensions
{
    public static int MoveWriteAsDecimal(this ref Span<byte> span, int number)
    {
        if (!number.TryFormat(span, out var written))
        {
            throw new Exception("Destination too short");
        }
        span = span[written..];
        return written;
    }
}