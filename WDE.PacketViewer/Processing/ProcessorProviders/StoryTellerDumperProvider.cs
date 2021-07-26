using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class StoryTellerDumperProvider : IPacketDumperProvider
    {
        private readonly Func<StoryTellerDumper> creator;

        public StoryTellerDumperProvider(Func<StoryTellerDumper> creator)
        {
            this.creator = creator;
        }
        
        public string Name => "Story teller";
        public string Description => "Presents sniff as human readable story (only some packets)";
        public string Extension => "story";
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(creator());
    }
}