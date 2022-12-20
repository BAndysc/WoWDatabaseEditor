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
    
    public IQuery Update(QuestChainDiff q)
    {
        if (!q.ExclusiveGroup.HasValue &&
            (!hasBreadcrumb || hasBreadcrumb && !q.BreadcrumbQuestId.HasValue) &&
            !q.NextQuestId.HasValue &&
            !q.PrevQuestId.HasValue)
            return Queries.Empty();

        var trans = Queries.BeginTransaction();

        var template = databaseProvider.GetQuestTemplate(q.Id);
        if (template != null)
            trans.Comment(template.Name);
        
        trans.Table(TableName)
            .InsertIgnore(new { ID = q.Id });
        
        var update = trans.Table(TableName)
            .Where(row => row.Column<uint>("ID") == q.Id)
            .ToUpdateQuery();

        if (q.PrevQuestId.HasValue)
            update = update.Set("PrevQuestID", q.PrevQuestId.Value);
        if (q.NextQuestId.HasValue)
            update = update.Set("NextQuestID", q.NextQuestId.Value);
        if (q.ExclusiveGroup.HasValue)
            update = update.Set("ExclusiveGroup", q.ExclusiveGroup.Value);
        if (hasBreadcrumb && q.BreadcrumbQuestId.HasValue)
            update = update.Set("BreadcrumbForQuestId", q.BreadcrumbQuestId.Value);
        
        update.Update();

        return trans.Close();
    }
}