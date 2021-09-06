using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [NonUniqueProvider]
    public interface IPacketDumperProvider
    {
        string Name { get; }
        string Description { get; }
        string Extension { get; }
        ImageUri? Image => null;
        Task<IPacketTextDumper> CreateDumper();
    }
}