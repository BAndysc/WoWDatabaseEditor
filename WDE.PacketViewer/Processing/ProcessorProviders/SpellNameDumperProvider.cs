using System;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class SpellNameDumperProvider : IPacketDumperProvider
    {
        private readonly Lazy<ISpellStore> spellStore;

        public SpellNameDumperProvider(Lazy<ISpellStore> spellStore)
        {
            this.spellStore = spellStore;
        }
        
        public string Name => "Spell name dump";
        public string Description => "Generate all unique spell names as c++ like enum";
        public string Extension => "cpp";
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(new NameAsEnumDumper(new SpellNameProcessor(spellStore.Value)));
    }
}