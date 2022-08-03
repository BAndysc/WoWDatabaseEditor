using System;
using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class QuestPacketRangesProvider : ITextPacketDumperProvider
    {
        private readonly Func<QuestPacketRangesProcessor> creator;

        public QuestPacketRangesProvider(Func<QuestPacketRangesProcessor> creator)
        {
            this.creator = creator;
        }

        public string Name => "Quest packet ranges";
        public string Description => "Prints when each quest was taken, completed and rewarded";
        public string Extension => "txt";
        public ImageUri? Image { get; } = new ImageUri("Icons/document_quest_template_big.png");
        public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) => Task.FromResult<IPacketTextDumper>(creator());
    }
}