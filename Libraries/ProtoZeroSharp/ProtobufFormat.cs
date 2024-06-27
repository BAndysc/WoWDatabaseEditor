using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProtoZeroSharp;

internal static class ProtobufFormat
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeKey(int fieldNumber, ProtoWireType wireType)
    {
        return ((ulong)fieldNumber << 3) | (ulong)wireType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int, ProtoWireType) DecodeKey(ulong key)
    {
        return ((int)(key >> 3), (ProtoWireType)(key & 0x7));
    }

    /// <summary>
    /// Returns the upper bound of the length of a var int field.
    /// </summary>
    internal const int VarIntFieldLenUpperBound = VarInt.MaxBytesCount * 2; // message id + value

    /// <summary>
    /// Returns the upper bound of the length of a submessage header.
    /// </summary>
    internal const int SubMessageHeaderLenUpperBound = VarInt.MaxBytesCount * 2; // message id + length

    /// <summary>
    /// Returns the upper bound of the length of a bytes field.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BytesFieldLenUpperBound(int payloadLength) => VarInt.MaxBytesCount * 2 + payloadLength; // message id + length + payload

    /// <summary>
    /// Writes a var int message to a buffer.
    /// The buffer must be at least 20 bytes long to fit any number.
    /// </summary>
    /// <param name="output">Output buffer to write to</param>
    /// <param name="fieldNumber">Proto message id</param>
    /// <param name="value">Value of the message</param>
    /// <returns>Number of bytes written</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarIntField(Span<byte> output, int fieldNumber, ulong value)
    {
        int written = VarInt.WriteVarint(output, EncodeKey(fieldNumber, ProtoWireType.VarInt));
        written += VarInt.WriteVarint(output.Slice(written), value);
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadFieldHeader(Span<byte> output, out int fieldNumber, out ProtoWireType wireType)
    {
        int read = VarInt.ReadVarint(output, out var encodedValue);
        (fieldNumber, wireType) = DecodeKey(encodedValue);
        return read;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteLengthFieldHeader(Span<byte> output, int messageId)
    {
        int written = VarInt.WriteVarint(output, EncodeKey(messageId, ProtoWireType.Length));
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteLengthFieldLength(Span<byte> output, int length)
    {
        int written = VarInt.WriteVarint(output, length);
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteLengthFieldLength(Span<byte> output, int length, int fixedBytesSize)
    {
        int written = VarInt.WriteVarintFixedSize(output, (ulong)length, fixedBytesSize);
        return written;
    }

    /// <summary>
    /// Writes a bytes buffer message to a buffer.
    /// The buffer must be at least 20 bytes + payload length long to fit any buffer.
    /// </summary>
    /// <param name="output">Output buffer to write to</param>
    /// <param name="fieldNumber">Proto message id</param>
    /// <param name="payload">Buffer to write</param>
    /// <returns>Number of bytes written</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteBytes(Span<byte> output, int fieldNumber, ReadOnlySpan<byte> payload)
    {
#if DEBUG
        if (output.Length < 20 + payload.Length)
            throw new ArgumentException($"Output buffer must be at least {20 + payload.Length} bytes long to fit any message");
#endif
        int written = VarInt.WriteVarint(output, EncodeKey(fieldNumber, ProtoWireType.Length));
        written += VarInt.WriteVarint(output.Slice(written), payload.Length);
        payload.CopyTo(output.Slice(written));
        written += payload.Length;
        return written;
    }

    /// <summary>
    /// Writes a string message to a buffer (encoded as UTF-8)
    /// The buffer must be at least 20 bytes + payload length long to fit any buffer.
    /// </summary>
    /// <param name="output">Output buffer to write to</param>
    /// <param name="fieldNumber">Proto message id</param>
    /// <param name="payloadBytesCount">Precalculatd number of bytes of utf-8 encoding of the given payload</param>
    /// <param name="payload">String to write</param>
    /// <returns>Number of bytes written</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe int WriteString(Span<byte> output, int fieldNumber, int payloadBytesCount, string payload)
    {
#if DEBUG
        if (output.Length < 20 + payload.Length)
            throw new ArgumentException($"Output buffer must be at least {20 + payload.Length} bytes long to fit any message");
#endif
        int written = VarInt.WriteVarint(output, EncodeKey(fieldNumber, ProtoWireType.Length));
        written += VarInt.WriteVarint(output.Slice(written), payloadBytesCount);
        fixed (char* payloadPtr = payload)
           fixed (byte* dst = output.Slice(written))
                Encoding.UTF8.GetBytes(payloadPtr, payload.Length, dst, payloadBytesCount);
        written += payload.Length;
        return written;
    }
}
