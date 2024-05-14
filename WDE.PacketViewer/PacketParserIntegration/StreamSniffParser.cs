using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration;

[AutoRegister]
public class StreamSniffParser : IStreamSniffParser
{
    private readonly IPacketParserService parserService;
    private readonly IMainThread mainThread;
    private ILivePacketParser? parser;

    public StreamSniffParser(IPacketParserService parserService,
        IMainThread mainThread)
    {
        this.parserService = parserService;
        this.mainThread = mainThread;
    }

    public async Task Start(CancellationToken token)
    {
        parser = await parserService.RunStreamParser(default, DumpFormatType.UniversalProtoWithText, token);
        parser.OnPacketParsed += packetString =>
        {
            var proto = Packets.Parser.ParseFrom(ByteString.FromBase64(packetString));
            mainThread.Dispatch(() =>
            {
                OnPacketArrived?.Invoke(proto);
            });
        };
    }

    public void Send(ParserRawPacket packet)
    {
        parser?.SendRawPacket(packet);
    }

    public event Action<Packets>? OnPacketArrived;
}