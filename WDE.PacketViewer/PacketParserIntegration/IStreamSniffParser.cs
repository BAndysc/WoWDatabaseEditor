using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.PacketViewer.Services;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration;

public interface IStreamSniffParser
{
    Task Start(CancellationToken token);
    void Send(ParserRawPacket packet);
    event Action<Packets> OnPacketArrived;
}