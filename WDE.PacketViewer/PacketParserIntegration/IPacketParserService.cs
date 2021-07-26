using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [UniqueProvider]
    public interface IPacketParserService
    {
        Task RunParser(string input, ParserConfiguration config, DumpFormatType dumpType, CancellationToken token, IProgress<float> progress);
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
        UniversalProtoWithText
    }
}