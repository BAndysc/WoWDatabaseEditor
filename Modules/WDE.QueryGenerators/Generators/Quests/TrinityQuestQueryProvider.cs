using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Quests;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "AzerothCore")]
public class TrinityQuestQueryProvider : IUpdateQueryProvider<QuestChainDiff>
{
    private readonly IDatabaseProvider databaseProvider;
    public string TableName => "quest_template_addon";
    private bool hasBreadcrumb;

    public TrinityQuestQueryProvider(IDatabaseProvider databaseProvider, ICurrentCoreVersion currentCoreVersion)
    {
        this.databaseProvider = databaseProvider;
        hasBreadcrumb = currentCoreVersion.Current.Tag != "AzerothCore";
    }
    
    public IQuery Update(QuestChainDiff diff)
    {
        if (!diff.ExclusiveGroup.HasValue &&
            (!hasBreadcrumb || hasBreadcrumb && !diff.BreadcrumbQuestId.HasValue) &&
            !diff.NextQuestId.HasValue &&
            !diff.PrevQuestId.HasValue)
            return Queries.Empty();

        var trans = Queries.BeginTransaction();

        var template = databaseProvider.GetQuestTemplate(diff.Id);
        if (template != null)
            trans.Comment(template.Name);
        
        trans.Table(TableName)
            .InsertIgnore(new { ID = diff.Id });
        
        var update = trans.Table(TableName)
            .Where(row => row.Column<uint>("ID") == diff.Id)
            .ToUpdateQuery();

        if (diff.PrevQuestId.HasValue)
            update = update.Set("PrevQuestID", diff.PrevQuestId.Value);
        if (diff.NextQuestId.HasValue)
            update = update.Set("NextQuestID", diff.NextQuestId.Value);
        if (diff.ExclusiveGroup.HasValue)
            update = update.Set("ExclusiveGroup", diff.ExclusiveGroup.Value);
        if (hasBreadcrumb && diff.BreadcrumbQuestId.HasValue)
            update = update.Set("BreadcrumbForQuestId", diff.BreadcrumbQuestId.Value);
        
        update.Update();

        return trans.Close();
    }
}