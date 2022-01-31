using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;

namespace WDE.QuestChainEditor.Documents;

[AutoRegister]
public class QuestChainQueryGenerator : ISolutionItemSqlProvider<QuestChainSolutionItem>
{
    private readonly IQuestTemplateSource questTemplateSource;
    private readonly IQueryGenerator queryGenerator;
    private readonly ISqlGenerator sqlGenerator;

    public QuestChainQueryGenerator(IQuestTemplateSource questTemplateSource,
        IQueryGenerator queryGenerator,
        ISqlGenerator sqlGenerator)
    {
        this.questTemplateSource = questTemplateSource;
        this.queryGenerator = queryGenerator;
        this.sqlGenerator = sqlGenerator;
    }
    
    public async Task<string> GenerateSql(QuestChainSolutionItem item)
    {
        var store = new QuestStore(questTemplateSource);
        foreach (var q in item.Entries)
            store.LoadQuest(q);
        var query = queryGenerator.Generate(store);
        return sqlGenerator.GenerateQuery(query.ToList(), item.ExistingData).QueryString;
    }
}