using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public interface IAsyncDatabaseProvider : IDatabaseProvider
    {
        Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync();
        Task<List<IConversationTemplate>> GetConversationTemplatesAsync();
        Task<List<IGameEvent>> GetGameEventsAsync();
        Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync();
        Task<List<IQuestTemplate>> GetQuestTemplatesAsync();
        Task<List<INpcText>> GetNpcTextsAsync();
        Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync();
    }
}