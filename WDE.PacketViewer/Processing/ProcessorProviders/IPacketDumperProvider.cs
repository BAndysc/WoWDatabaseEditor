using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [NonUniqueProvider]
    public interface IPacketDumperProvider
    {
        string Name { get; }
        string Description { get; }
        string Extension { get; }
        Task<IPacketTextDumper> CreateDumper();
    }
}