using System.Text;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public class ToTextProcessor : IPacketToTextProcessor
    {
        private readonly IPacketProcessor<bool> inner;
        private readonly StringBuilder sb;

        public ToTextProcessor(IPacketProcessor<bool> inner, StringBuilder sb)
        {
            this.inner = inner;
            this.sb = sb;
        }
        
        public ToTextProcessor(IPacketProcessor<Nothing> inner, StringBuilder sb)
        {
            this.inner = new NothingToBoolProcessor(inner);
            this.sb = sb;
        }
        
        public bool Process(PacketHolder packet)
        {
            return inner.Process(packet);
        }

        public string Generate()
        {
            return sb.ToString();
        }
    }
}