using System;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.RemoteEnvironment;

internal readonly struct RemoteVisualStudioHeader
{
    public const int BufferLength = 5;
    public readonly RemoteVisualStudioPacketType Type;
    public readonly int BytesLength;

    public RemoteVisualStudioHeader(ReadOnlySpan<byte> source)
    {
        Type = (RemoteVisualStudioPacketType)source[0];
        BytesLength = BitConverter.ToInt32(source.Slice(1, 4));
    }

    public RemoteVisualStudioHeader(RemoteVisualStudioPacketType type, int bytesLength)
    {
        Type = type;
        BytesLength = bytesLength;
    }
}