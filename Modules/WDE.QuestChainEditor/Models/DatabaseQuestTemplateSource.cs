using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Models;

[AutoRegister]
public class DatabaseQuestTemplateSource : IQuestTemplateSource
{
    private readonly IDatabaseProvider databaseProvider;

    public DatabaseQuestTemplateSource(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public Task<IQuestTemplate?> GetTemplate(uint entry)
    {
        return databaseProvider.GetQuestTemplate(entry);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByExclusiveGroup(int exclusiveGroup)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.ExclusiveGroup == exclusiveGroup);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByPreviousQuestId(uint previous)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.PrevQuestId == previous || qt.PrevQuestId == -(int)previous);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByNextQuestId(uint previous)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.NextQuestId == previous);
    }

    public async Task<IEnumerable<IQuestTemplate>> GetByBreadCrumbQuestId(uint questId)
    {
        // optimize this!
        return (await databaseProvider.GetQuestTemplatesAsync()).Where(qt => qt.BreadcrumbForQuestId == questId);
    }
}