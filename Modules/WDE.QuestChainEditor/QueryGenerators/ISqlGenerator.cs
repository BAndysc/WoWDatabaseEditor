using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QuestChainEditor.QueryGenerators;

[UniqueProvider]
public interface ISqlGenerator
{
    IQuery GenerateQuery(IList<ChainRawData> data, IReadOnlyDictionary<uint, ChainRawData>? existing);
}

[AutoRegister]
[SingleInstance]
public class TrinitySqlGenerator : ISqlGenerator
{
    private readonly IDatabaseProvider databaseProvider;

    public TrinitySqlGenerator(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public IQuery GenerateQuery(IList<ChainRawData> data, IReadOnlyDictionary<uint, ChainRawData>? existing)
    {
        var trans = Queries.BeginTransaction();

        foreach (var q in data)
        {
            var update = trans.Table("quest_template_addon")
                .Where(row => row.Column<uint>("ID") == q.Id)
                .ToUpdateQuery();

            if (existing == null || !existing.TryGetValue(q.Id, out var existingData))
            {
                update = update.Set("PrevQuestID", q.PrevQuestId)
                    .Set("NextQuestID", q.NextQuestId)
                    .Set("ExclusiveGroup", q.ExclusiveGroup);
            }
            else
            {
                if (q.PrevQuestId != existingData.PrevQuestId)
                    update = update.Set("PrevQuestID", q.PrevQuestId);
                if (q.NextQuestId != existingData.NextQuestId)
                    update = update.Set("NextQuestID", q.NextQuestId);
                if (q.ExclusiveGroup != existingData.ExclusiveGroup)
                    update = update.Set("ExclusiveGroup", q.ExclusiveGroup);
            }

            if (!update.Empty)
            {
                var template = databaseProvider.GetQuestTemplate(q.Id);
                if (template != null)
                    trans.Comment(template.Name);
                update.Update();
            }
        }

        return trans.Close();
    }
}