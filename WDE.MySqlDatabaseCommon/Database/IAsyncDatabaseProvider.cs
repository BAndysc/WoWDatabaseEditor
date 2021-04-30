using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database
{
    public interface IAsyncDatabaseProvider : IDatabaseProvider
    {
        Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync();
        Task<List<IConversationTemplate>> GetConversationTemplatesAsync();
        Task<List<IGameEvent>> GetGameEventsAsync();
        Task<List<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync();
        Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync();
        Task<List<IQuestTemplate>> GetQuestTemplatesAsync();
        Task<List<IGossipMenu>> GetGossipMenusAsync();
        Task<List<INpcText>> GetNpcTextsAsync();
    }
}