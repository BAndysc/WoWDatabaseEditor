using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using ProtoZeroSharp;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;
using WDE.PacketViewer.Utils;
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

    public async Task Start(RefCountedArena memory, CancellationToken token)
    {
        parser = await parserService.RunStreamParser(default, DumpFormatType.UniversalProtoWithText, token);
        parser.OnPacketParsed += packetString =>
        {
            unsafe
            {
                using var @ref = memory.Increment();
                var bytes = Convert.FromBase64String(packetString);
                var packets = new Packets();
                fixed (byte* ptr = bytes)
                {
                    var reader = new ProtoReader(ptr, bytes.Length);
                    packets.Read(ref reader, ref @ref.Array);
                }
                mainThread.Dispatch(() =>
                {
                    OnPacketArrived?.Invoke(packets);
                });
            }
        };
    }

    public void Send(ParserRawPacket packet)
    {
        parser?.SendRawPacket(packet);
    }

    public event Action<Packets>? OnPacketArrived;
}