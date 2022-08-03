using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class StoryTellerDumperProvider : ITextPacketDumperProvider
    {
        private readonly IContainerProvider containerProvider;

        public StoryTellerDumperProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        
        public string Name => "Story teller";
        public string Description => "Presents sniff as human readable story (only some packets)";
        public string Extension => "story";
        public bool RequiresSplitUpdateObject => true;
        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) =>
            Task.FromResult<IPacketTextDumper>(containerProvider.Resolve<StoryTellerDumper>((typeof(bool), false), (typeof(IParsingSettings), settings)));
    }
    
    [AutoRegister]
    public class PerGuidStoryTellerDumperProvider : ITextPacketDumperProvider
    {
        private readonly IContainerProvider containerProvider;

        public PerGuidStoryTellerDumperProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        
        public string Name => "Story per guid";
        public string Description => "Presents sniff as human readable story (only some packets), grouped by each guid";
        public string Extension => "story";
        public bool RequiresSplitUpdateObject => true;
        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) =>
            Task.FromResult<IPacketTextDumper>(containerProvider.Resolve<StoryTellerDumper>((typeof(bool), true), (typeof(IParsingSettings), settings)));
    }
}