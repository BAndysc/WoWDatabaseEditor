using System;
using System.Threading.Tasks;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    public interface ISniffLoader
    {
        Task<Packets> LoadSniff(string path, IProgress<float> progress);
    }
}