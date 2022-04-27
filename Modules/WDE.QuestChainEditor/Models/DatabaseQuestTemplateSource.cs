using System.Collections.Generic;
using System.Linq;
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
    
    public IQuestTemplate? GetTemplate(uint entry)
    {
        return databaseProvider.GetQuestTemplate(entry);
    }

    public IEnumerable<IQuestTemplate> GetByPreviousQuestId(uint previous)
    {
        // optimize this!
        return databaseProvider.GetQuestTemplates().Where(qt => qt.PrevQuestId == previous || qt.PrevQuestId == -(int)previous);
    }

    public IEnumerable<IQuestTemplate> GetByNextQuestId(uint previous)
    {
        // optimize this!
        return databaseProvider.GetQuestTemplates().Where(qt => qt.NextQuestId == previous);
    }

    public IEnumerable<IQuestTemplate> GetByBreadCrumbQuestId(uint questId)
    {
        // optimize this!
        return databaseProvider.GetQuestTemplates().Where(qt => qt.BreadcrumbForQuestId == questId);
    }
}