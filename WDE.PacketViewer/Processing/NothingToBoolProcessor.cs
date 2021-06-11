using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public class NothingToBoolProcessor : IPacketProcessor<bool>
    {
        private readonly IPacketProcessor<Nothing> nothingProcessor;

        public NothingToBoolProcessor(IPacketProcessor<Nothing> nothingProcessor)
        {
            this.nothingProcessor = nothingProcessor;
        }
        
        public bool Process(PacketHolder packet)
        {
            nothingProcessor.Process(packet);
            return true;
        }
    }
}