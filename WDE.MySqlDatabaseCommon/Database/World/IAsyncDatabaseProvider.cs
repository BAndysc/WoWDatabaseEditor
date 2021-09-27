using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public interface IAsyncDatabaseProvider : IDatabaseProvider
    {
        Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync();
        Task<List<IConversationTemplate>> GetConversationTemplatesAsync();
        Task<List<IGameEvent>> GetGameEventsAsync();
        Task<List<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync();
        Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync();
        Task<List<IQuestTemplate>> GetQuestTemplatesAsync();
        Task<List<INpcText>> GetNpcTextsAsync();
        Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync();
        Task<List<IBroadcastText>> GetBroadcastTextsAsync();
    }
}