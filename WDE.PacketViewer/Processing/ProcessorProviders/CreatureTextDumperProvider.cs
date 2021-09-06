using System;
using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class CreatureTextDumperProvider : IPacketDumperProvider
    {
        private readonly Func<CreatureTextDumper> creator;

        public CreatureTextDumperProvider(Func<CreatureTextDumper> creator)
        {
            this.creator = creator;
        }
        public string Name => "Creature text";
        public string Description => "Generate all creature texts with emotes and sounds";
        public string Extension => "sql";
        public ImageUri? Image { get; } = new ImageUri("icons/chat_big.png");
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(creator());
    }
}