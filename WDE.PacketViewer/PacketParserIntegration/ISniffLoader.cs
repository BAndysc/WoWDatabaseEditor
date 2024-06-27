using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    public unsafe interface ISniffLoader
    {
        Task<Packets> LoadSniff(RefCountedArena allocator, string path, int? customVersion, CancellationToken token, bool withText, IProgress<float> progress);
    }
}