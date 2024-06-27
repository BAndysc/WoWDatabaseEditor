using WowPacketParser.Proto;
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

        public void Initialize(ulong gameBuild)
        {
            nothingProcessor.Initialize(gameBuild);
        }

        public bool Process(ref readonly PacketHolder packet)
        {
            nothingProcessor.Process(in packet);
            return true;
        }
    }
}