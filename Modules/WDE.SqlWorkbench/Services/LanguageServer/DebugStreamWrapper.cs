using System.IO;

namespace WDE.SqlWorkbench.Services.LanguageServer;

internal class DebugStreamWrapper : Stream
{
    private readonly Stream inner, debugOut;
    private static byte[] DebugReading = "[Reading] "u8.ToArray();
    private static byte[] DebugSeeking = "[Seeking] "u8.ToArray();
    private static byte[] DebugSetLength = "[SetLength] "u8.ToArray();
    private static byte[] DebugWriting = "[Writing] "u8.ToArray();
    
    public bool DebugEnabled { get; set; } = true;

    public DebugStreamWrapper(Stream inner, Stream debugOut)
    {
        this.inner = inner;
        this.debugOut = debugOut;
    }

    public override void Flush()
    {
        inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var c = inner.Read(buffer, offset, count);
        if (DebugEnabled)
        {
            debugOut.Write(DebugReading);
            debugOut.Write(buffer, offset, c);
        }
        return c;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (DebugEnabled)
            debugOut.Write(DebugSeeking);
        return inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        if (DebugEnabled)
            debugOut.Write(DebugSetLength);
        inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (DebugEnabled)
        {
            debugOut.Write(DebugWriting);
            debugOut.Write(buffer, offset, count);
        }
        inner.Write(buffer, offset, count);
    }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => inner.CanWrite;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }
}