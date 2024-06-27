using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class CreatureTextDumperProvider : ITextPacketDumperProvider
    {
        private readonly IContainerProvider containerProvider;

        public CreatureTextDumperProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        public string Name => "Creature text";
        public string Description => "Generate all creature texts with emotes and sounds";
        public string Extension => "sql";
        public bool CanProcessMultipleFiles => true;
        public ImageUri? Image { get; } = new ImageUri("Icons/chat_big.png");
        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) =>
            Task.FromResult<IPacketTextDumper>(containerProvider.Resolve<CreatureTextDumper>((typeof(bool), false), (typeof(IParsingSettings), settings)));
    }
    
    [AutoRegister]
    public class CreatureTextDiffDumperProvider : ITextPacketDumperProvider
    {
        private readonly IContainerProvider containerProvider;

        public CreatureTextDiffDumperProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        public string Name => "Creature text (diff)";
        public string Description => "Generate all creature texts with emotes and sounds, as a diff with current texts in the database";
        public string Extension => "sql";
        public bool CanProcessMultipleFiles => true;
        public ImageUri? Image { get; } = new ImageUri("Icons/chat_diff_big.png");
        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) =>
            Task.FromResult<IPacketTextDumper>(containerProvider.Resolve<CreatureTextDumper>((typeof(bool), true), (typeof(IParsingSettings), settings)));
    }
}