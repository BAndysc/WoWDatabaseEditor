using System;
using System.Text;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.RemoteEnvironment;

internal readonly struct RemoteVisualStudioPacket
{
    public readonly RemoteVisualStudioHeader Header;
    public readonly string Message;

    public RemoteVisualStudioPacket(ReadOnlySpan<byte> source)
    {
        Header = new RemoteVisualStudioHeader(source);
        Message = Encoding.UTF8.GetString(source.Slice(5));
    }

    public RemoteVisualStudioPacket(RemoteVisualStudioHeader header, ReadOnlySpan<byte> source)
    {
        Header = header;
        Message = Encoding.UTF8.GetString(source);
    }

    public RemoteVisualStudioPacket(RemoteVisualStudioPacketType packetType, string message)
    {
        Header = new RemoteVisualStudioHeader(packetType, Encoding.UTF8.GetByteCount(message));
        Message = message;
    }

    public int BufferLength => Header.BytesLength + RemoteVisualStudioHeader.BufferLength;

    public void ToBytes(Span<byte> output)
    {
        if (output.Length != BufferLength)
            throw new ArgumentException("Output span length must be equal to packet length");

        output[0] = (byte)Header.Type;
        BitConverter.GetBytes(Header.BytesLength).CopyTo(output.Slice(1, 4));
        Encoding.UTF8.GetBytes(Message, output.Slice(5));
    }

    public byte[] ToBytes()
    {
        byte[] result = new byte[BufferLength];
        ToBytes(result);
        return result;
    }

    public static RemoteVisualStudioPacket Execute(string message)
    {
        return new RemoteVisualStudioPacket(RemoteVisualStudioPacketType.Execute, message);
    }
}