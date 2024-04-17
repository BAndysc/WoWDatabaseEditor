using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Services;

[UniqueProvider]
public interface IQuestChainsServerIntegrationService
{
    bool IsSupported { get; }
    Task<IReadOnlyList<string>> GetPlayersAsync();
    Task<IReadOnlyList<QuestState>> GetQuestStatesAsync(string playerName, IReadOnlyList<uint> quests);
    Task CompleteQuests(string playerName, IReadOnlyList<uint> quests);
    Task RewardQuests(string playerName, IReadOnlyList<uint> quests);
    Task RemoveQuests(string playerName, IReadOnlyList<uint> quests);
    Task AddQuests(string playerName, IReadOnlyList<uint> quests);
    Task TeleportPlayer(string playerName, int mapId, float x, float y, float z, float o);
}