using System;

namespace WDE.PacketViewer.Services;

public class ParserRawPacket
{
    public bool IsSmsg { get; init; }
    public int SessionId { get; init; }
    public ulong TickCount { get; init; }
    public int Opcode { get; init; }
    public byte[] Data { get; init; } = Array.Empty<byte>();
}