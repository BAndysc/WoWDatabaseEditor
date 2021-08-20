using System;
using System.Threading;
using System.Threading.Tasks;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    public interface ISniffLoader
    {
        Task<Packets> LoadSniff(string path, CancellationToken token, IProgress<float> progress);
    }
}