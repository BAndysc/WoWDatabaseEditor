using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Quests;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class MangosQuestQueryProvider : IUpdateQueryProvider<QuestChainDiff>
{
    private readonly IDatabaseProvider databaseProvider;
    public string TableName => "quest_template";

    public MangosQuestQueryProvider(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public IQuery Update(QuestChainDiff q)
    {
        if (!q.ExclusiveGroup.HasValue &&
            (!q.BreadcrumbQuestId.HasValue) &&
            !q.NextQuestId.HasValue &&
            !q.PrevQuestId.HasValue)
            return Queries.Empty();

        var trans = Queries.BeginTransaction();

        trans.Table(TableName)
            .InsertIgnore(new { entry = q.Id });
        
        var update = trans.Table(TableName)
            .Where(row => row.Column<uint>("entry") == q.Id)
            .ToUpdateQuery();

        if (q.PrevQuestId.HasValue)
            update = update.Set("PrevQuestId", q.PrevQuestId.Value);
        if (q.NextQuestId.HasValue)
            update = update.Set("NextQuestId", q.NextQuestId.Value);
        if (q.ExclusiveGroup.HasValue)
            update = update.Set("ExclusiveGroup", q.ExclusiveGroup.Value);
        if (q.BreadcrumbQuestId.HasValue)
            update = update.Set("BreadcrumbForQuestId", q.BreadcrumbQuestId.Value);
        
        var template = databaseProvider.GetQuestTemplate(q.Id);
        if (template != null)
            trans.Comment(template.Name);
        update.Update();

        return trans.Close();
    }
}