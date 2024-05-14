using System;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.PacketViewer.Services;

public interface ISnifferProxy
{
    event Action<ParserRawPacket>? OnSnifferPacketArrived;
    Task Listen(CancellationToken token);
}