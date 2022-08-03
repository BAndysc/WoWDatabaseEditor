using System;
using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class GossipExtractProvider : ITextPacketDumperProvider
    {
        private readonly Func<GossipExtractProcessor> processor;

        public GossipExtractProvider(Func<GossipExtractProcessor> processor)
        {
            this.processor = processor;
        }
        
        public string Name => "Gossip menus";
        public string Description => "Generates query for gossip_menu and gossip_menu_option tables";
        public string Extension => "sql";
        public ImageUri? Image => new ImageUri("Icons/gossip.png");
        public bool CanProcessMultipleFiles => true;

        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings)
        {
            return Task.FromResult<IPacketTextDumper>(processor());
        }
    }
}