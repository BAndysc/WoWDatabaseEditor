using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public interface IPacketTextDumper : IPacketProcessor<bool>
    {
        string Generate();
    }
}