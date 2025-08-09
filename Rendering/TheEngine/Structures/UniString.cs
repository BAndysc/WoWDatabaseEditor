using System.Text;

namespace TheEngine;

public struct UniString
{
    private string? asString;
    private byte[]? asUtf8;

    public ReadOnlySpan<byte> CString => asUtf8 == null ? default : asUtf8.AsSpan();
    public string? String => asString;

    public UniString(ReadOnlySpan<byte> utf8Bytes)
    {
        if (utf8Bytes == default)
        {
            asString = null;
            asUtf8 = null;
        }
        else
        {
            if (utf8Bytes[^1] != 0)
                throw new Exception("Null terminated string is expected");
            asString = Encoding.UTF8.GetString(utf8Bytes);
            asUtf8 = new byte[utf8Bytes.Length];
            utf8Bytes.CopyTo(asUtf8);
        }
    }

    public UniString(string? s)
    {
        if (s == null)
        {
            asString = null;
            asUtf8 = null;
        }
        else
        {
            asString = s;
            var byteCount = Encoding.UTF8.GetByteCount(s);
            asUtf8 = new byte[byteCount + 1]; // c-string terminator
            Encoding.UTF8.GetBytes(s, asUtf8);
        }
    }

    public static implicit operator UniString(string value)
    {
        return new UniString(value);
    }

    public static implicit operator UniString(ReadOnlySpan<byte> value)
    {
        return new UniString(value);
    }

    public static implicit operator ReadOnlySpan<byte>(UniString value)
    {
        return value.CString;
    }
}