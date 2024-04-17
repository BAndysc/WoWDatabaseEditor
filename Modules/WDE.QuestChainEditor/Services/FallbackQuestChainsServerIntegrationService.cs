using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Services;

[FallbackAutoRegister]
[SingleInstance]
public class FallbackQuestChainsServerIntegrationService : IQuestChainsServerIntegrationService
{
    public bool IsSupported => false;

    public async Task<IReadOnlyList<string>> GetPlayersAsync()
    {
        throw new System.NotImplementedException();
    }

    public async Task<IReadOnlyList<QuestState>> GetQuestStatesAsync(string playerName, IReadOnlyList<uint> quests)
    {
        throw new System.NotImplementedException();
    }

    public async Task CompleteQuests(string playerName, IReadOnlyList<uint> quests)
    {
        throw new System.NotImplementedException();
    }

    public async Task RewardQuests(string playerName, IReadOnlyList<uint> quests)
    {
        throw new System.NotImplementedException();
    }

    public async Task RemoveQuests(string playerName, IReadOnlyList<uint> quests)
    {
        throw new System.NotImplementedException();
    }

    public async Task AddQuests(string playerName, IReadOnlyList<uint> quests)
    {
        throw new System.NotImplementedException();
    }

    public async Task TeleportPlayer(string playerName, int mapId, float x, float y, float z, float o)
    {
        throw new System.NotImplementedException();
    }
}