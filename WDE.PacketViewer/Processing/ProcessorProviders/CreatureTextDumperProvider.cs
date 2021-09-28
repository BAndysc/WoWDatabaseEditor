using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class CreatureTextDumperProvider : IPacketDumperProvider
    {
        private readonly IContainerProvider containerProvider;

        public CreatureTextDumperProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        public string Name => "Creature text";
        public string Description => "Generate all creature texts with emotes and sounds";
        public string Extension => "sql";
        public ImageUri? Image { get; } = new ImageUri("icons/chat_big.png");
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(containerProvider.Resolve<CreatureTextDumper>((typeof(bool), false)));
    }
    
    [AutoRegister]
    public class CreatureTextDiffDumperProvider : IPacketDumperProvider
    {
        private readonly IContainerProvider containerProvider;

        public CreatureTextDiffDumperProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        public string Name => "Creature text (diff)";
        public string Description => "Generate all creature texts with emotes and sounds, as a diff with current texts in the database";
        public string Extension => "sql";
        public ImageUri? Image { get; } = new ImageUri("icons/chat_diff_big.png");
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(containerProvider.Resolve<CreatureTextDumper>((typeof(bool), true)));
    }
}