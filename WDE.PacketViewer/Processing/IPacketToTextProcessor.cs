using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public interface IPacketToTextProcessor : IPacketProcessor<bool>
    {
        string Generate();
    }
}