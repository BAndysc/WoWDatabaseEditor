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
    private readonly ICachedDatabaseProvider databaseProvider;
    public DatabaseTable TableName => DatabaseTable.WorldTable("quest_template");

    public MangosQuestQueryProvider(ICachedDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public IQuery Update(QuestChainDiff diff)
    {
        if (!diff.ExclusiveGroup.HasValue &&
            (!diff.BreadcrumbQuestId.HasValue) &&
            !diff.NextQuestId.HasValue &&
            !diff.PrevQuestId.HasValue)
            return Queries.Empty(DataDatabaseType.World);

        var trans = Queries.BeginTransaction(DataDatabaseType.World);

        trans.Table(TableName)
            .InsertIgnore(new { entry = diff.Id });
        
        var update = trans.Table(TableName)
            .Where(row => row.Column<uint>("entry") == diff.Id)
            .ToUpdateQuery();

        if (diff.PrevQuestId.HasValue)
            update = update.Set("PrevQuestId", diff.PrevQuestId.Value);
        if (diff.NextQuestId.HasValue)
            update = update.Set("NextQuestId", diff.NextQuestId.Value);
        if (diff.ExclusiveGroup.HasValue)
            update = update.Set("ExclusiveGroup", diff.ExclusiveGroup.Value);
        if (diff.BreadcrumbQuestId.HasValue)
            update = update.Set("BreadcrumbForQuestId", diff.BreadcrumbQuestId.Value);
        
        var template = databaseProvider.GetCachedQuestTemplate(diff.Id);
        if (template != null)
            trans.Comment(template.Name);
        update.Update();

        return trans.Close();
    }
}