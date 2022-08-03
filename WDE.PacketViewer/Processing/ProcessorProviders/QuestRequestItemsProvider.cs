using System;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Processing.Processors.Utils;

namespace WDE.PacketViewer.Processing.ProcessorProviders;

[AutoRegister]
public class QuestRequestItemsProvider : ITextPacketDumperProvider
{
    private readonly Func<QuestRequestItemsProcessor> creator;

    public QuestRequestItemsProvider(Func<QuestRequestItemsProcessor> creator)
    {
        this.creator = creator;
    }
    public string Name => "Quest request items";
    public string Description => "Generates a dump for quest_request_items (diff)";
    public string Extension => "sql";
    public bool CanProcessMultipleFiles => true;
    public ImageUri? Image { get; } = new ImageUri("Icons/document_quest_big.png");
    public Task<IPacketTextDumper> CreateDumper(IParsingSettings settings) =>
        Task.FromResult<IPacketTextDumper>(creator());
}