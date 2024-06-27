using System.Text;
using System.Threading.Tasks;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public class TextDumper : IPacketTextDumper
    {
        private readonly IPacketProcessor<bool> inner;
        private readonly StringBuilder sb;

        public TextDumper(IPacketProcessor<bool> inner, StringBuilder sb)
        {
            this.inner = inner;
            this.sb = sb;
        }
        
        public TextDumper(IPacketProcessor<Nothing> inner, StringBuilder sb)
        {
            this.inner = new NothingToBoolProcessor(inner);
            this.sb = sb;
        }

        public void Initialize(ulong gameBuild)
        {
            inner.Initialize(gameBuild);
        }

        public bool Process(ref readonly PacketHolder packet)
        {
            return inner.Process(in packet);
        }

        public async Task<string> Generate()
        {
            return sb.ToString();
        }
    }
}