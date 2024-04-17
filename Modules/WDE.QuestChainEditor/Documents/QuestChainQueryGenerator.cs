using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;
using WDE.QuestChainEditor.Services;
using WDE.SqlQueryGenerator;

namespace WDE.QuestChainEditor.Documents;

[AutoRegister]
public class QuestChainQueryGenerator : ISolutionItemSqlProvider<QuestChainSolutionItem>
{
    private readonly IQuestTemplateSource questTemplateSource;
    private readonly IQuestChainLoader questChainLoader;
    private readonly IChainGenerator chainGenerator;
    private readonly IQueryGenerator queryGenerator;

    public QuestChainQueryGenerator(IQuestTemplateSource questTemplateSource,
        IQuestChainLoader questChainLoader,
        IChainGenerator chainGenerator,
        IQueryGenerator queryGenerator)
    {
        this.questTemplateSource = questTemplateSource;
        this.questChainLoader = questChainLoader;
        this.chainGenerator = chainGenerator;
        this.queryGenerator = queryGenerator;
    }
    
    public async Task<IQuery> GenerateSql(QuestChainSolutionItem item)
    {
        var store = new QuestStore();
        foreach (var q in item.Entries)
            await questChainLoader.LoadChain(q, store);
        var query = await chainGenerator.Generate(store, item.ExistingData);
        var conditions = store.Where(x => x.Conditions.Count > 0)
            .ToDictionary(x => x.Entry, x => x.Conditions);
        return await queryGenerator.GenerateQuery(query.ToList(), item.ExistingData, conditions, item.ExistingConditions
            .ToDictionary(x => x.Key, x => (IReadOnlyList<ICondition>)x.Value));
    }
}