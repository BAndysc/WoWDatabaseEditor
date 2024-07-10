using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors;

[AutoRegister]
public class QuestRequestItemsProcessor : PacketProcessor<bool>, IPacketTextDumper
{
    private readonly IDatabaseProvider databaseProvider;

    private Dictionary<uint, string> titles = new();
    private Dictionary<uint, string> completionTexts = new();
    private Dictionary<uint, int> emotesCompleted = new();
    private Dictionary<uint, int> emotesNonCompleted = new();

    public QuestRequestItemsProcessor(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }
    
    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverRequestItems packet)
    {
        completionTexts[packet.QuestId] = packet.CompletionText.ToString() ?? "";
        bool isComplete = packet.StatusFlags.HasFlagFast(PacketQuestStatusFlags.KillCreditComplete) &&
                          packet.StatusFlags.HasFlagFast(PacketQuestStatusFlags.CollectableComplete) &&
                          packet.StatusFlags.HasFlagFast(PacketQuestStatusFlags.QuestStatusUnk8) &&
                          packet.StatusFlags.HasFlagFast(PacketQuestStatusFlags.QuestStatusUnk16) &&
                          packet.StatusFlags.HasFlagFast(PacketQuestStatusFlags.QuestStatusUnk64) &&
                          packet.StatusFlags.HasFlagFast(PacketQuestStatusFlags.QuestStatusUnk128);
        titles[packet.QuestId] = packet.QuestTitle.ToString() ?? "";
        if (isComplete)
            emotesCompleted[packet.QuestId] = packet.EmoteType;
        else
            emotesNonCompleted[packet.QuestId] = packet.EmoteType;
        return base.Process(in basePacket, in packet);
    }

    public async Task<string> Generate()
    {
        var q = Queries.BeginTransaction(DataDatabaseType.World);
        foreach (var id in completionTexts.Keys)
        {
            int? emoteCompleted = null;
            int? emoteNonCompleted = null;
            if (emotesCompleted.TryGetValue(id, out int emote))
                emoteCompleted = emote;
            if (emotesNonCompleted.TryGetValue(id, out int emote2))
                emoteNonCompleted = emote2;

            var completionText = completionTexts[id];
            var title = titles[id];

            var existing = await databaseProvider.GetQuestRequestItem(id);

            if (existing == null)
            {
                q.Table(DatabaseTable.WorldTable("quest_request_items")).Where(row => row.Column<uint>("ID") == id).Delete();
                q.Table(DatabaseTable.WorldTable("quest_request_items"))
                    .Insert(new
                    {
                        ID = id,
                        EmoteOnComplete = emoteCompleted ?? 0,
                        EmoteOnIncomplete = emoteNonCompleted ?? 0,
                        CompletionText = completionText,
                        __comment = title
                    });
            }
            else
            {
                var update = q.Table(DatabaseTable.WorldTable("quest_request_items"))
                    .Where(row => row.Column<uint>("ID") == id).ToUpdateQuery();

                if (existing.EmoteOnComplete != emoteCompleted && emoteCompleted.HasValue)
                    update = update.Set("EmoteOnComplete", emoteCompleted.Value);
                
                if (existing.EmoteOnIncomplete != emoteCompleted && emoteNonCompleted.HasValue)
                    update = update.Set("EmoteOnIncomplete", emoteNonCompleted.Value);
                
                if (existing.CompletionText != completionTexts[id])
                    update = update.Set("CompletionText", completionText);
                
                if (!update.Empty)
                    update.Update(title);
            }
        }

        return q.Close().QueryString;
    }
}
