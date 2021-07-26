using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class CreatureGameObjectNameDumperProvider : IPacketDumperProvider
    {
        public CreatureGameObjectNameDumperProvider()
        {
        }
        public string Name => "Creature/gameobject name dump";
        public string Description => "Generate all unique creature and gameobject names as c++ like enum";
        public string Extension => "cpp";
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(new NameAsEnumDumper(new CreatureGameObjectNameProcessor()));
    }
}