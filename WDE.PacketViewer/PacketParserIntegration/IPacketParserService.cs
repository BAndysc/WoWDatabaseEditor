using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [UniqueProvider]
    public interface IPacketParserService
    {
        Task RunParser(string input, ParserConfiguration config, DumpFormatType dumpType, int? customVersion, CancellationToken token, IProgress<float> progress);
        Task<ILivePacketParser> RunStreamParser(ParserConfiguration config, DumpFormatType dumpType, CancellationToken token);
    }

    public interface ILivePacketParser
    {
        void SendRawPacket(ParserRawPacket packet);
        event Action<string>? OnPacketParsed;
    }
    
    public enum DumpFormatType
    {
        None,           // No dump at all
        Text,
        Pkt,
        PktSplit,
        SqlOnly,
        SniffDataOnly,
        StatisticsPreParse,
        PktSessionSplit,
        CompressSniff,
        SniffVersionSplit,
        HexOnly,
        PktDirectionSplit,
        ConnectionIndexes,
        Fusion,
        UniversalProto,
        UniversalProtoWithText,
        UniversalProtoWithSeparateText
    }
}