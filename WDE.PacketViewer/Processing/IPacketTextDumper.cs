using System.Threading.Tasks;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public interface IPacketTextDumper : IPacketProcessor<bool>
    {
        Task<string> Generate();
    }
    
    public interface ITwoStepPacketBoolProcessor : ITwoStepPacketProcessor<bool>
    {
    }
    
    public interface ITwoStepPacketProcessor<T>
    {
        T? PreProcess(PacketHolder packet);
    }
}