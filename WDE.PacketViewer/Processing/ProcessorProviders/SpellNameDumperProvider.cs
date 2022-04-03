using System;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class SpellNameDumperProvider : IPacketDumperProvider
    {
        private readonly Func<SpellNameProcessor> processor;

        public SpellNameDumperProvider(Func<SpellNameProcessor> processor)
        {
            this.processor = processor;
        }
        
        public string Name => "Spell and sound names";
        public string Description => "Generate all unique spell names as c++ like enum";
        public string Extension => "cpp";
        public ImageUri? Image { get; } = new ImageUri("Icons/document_spell_big.png");
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(new NameAsEnumDumper(processor()));
    }
}