using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class CreatureGameObjectNameDumperProvider : ITextPacketDumperProvider
    {
        public CreatureGameObjectNameDumperProvider()
        {
        }
        public string Name => "Creature/gameobject name dump";
        public string Description => "Generate all unique creature and gameobject names as c++ like enum";
        public string Extension => "cpp";
        public bool CanProcessMultipleFiles => true;
        public ImageUri? Image { get; } = new ImageUri("Icons/document_creature_template_big.png");
        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) =>
            Task.FromResult<IPacketTextDumper>(new NameAsEnumDumper(new CreatureGameObjectNameProcessor()));
    }
}