using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;
using WDE.QuestChainEditor.Services;
using WDE.QuestChainEditor.ViewModels;
using WDE.SqlQueryGenerator;

namespace WDE.QuestChainEditor.Documents;

[AutoRegisterToParentScope]
public class QuestChainQueryGenerator : ISolutionItemSqlProvider<QuestChainSolutionItem>
{
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly StandaloneQuestChainEditorService standaloneQuestChainEditorService;
    private readonly IQuestTemplateSource questTemplateSource;
    private readonly IQuestChainLoader questChainLoader;
    private readonly IChainGenerator chainGenerator;
    private readonly IQueryGenerator queryGenerator;

    public QuestChainQueryGenerator(Lazy<IDocumentManager> documentManager,
        StandaloneQuestChainEditorService standaloneQuestChainEditorService,
        IQuestTemplateSource questTemplateSource,
        IQuestChainLoader questChainLoader,
        IChainGenerator chainGenerator,
        IQueryGenerator queryGenerator)
    {
        this.documentManager = documentManager;
        this.standaloneQuestChainEditorService = standaloneQuestChainEditorService;
        this.questTemplateSource = questTemplateSource;
        this.questChainLoader = questChainLoader;
        this.chainGenerator = chainGenerator;
        this.queryGenerator = queryGenerator;
    }
    
    public async Task<IQuery> GenerateSql(QuestChainSolutionItem item)
    {
        var openedEditors = documentManager.Value.OpenedDocuments.Where(doc => doc is QuestChainDocumentViewModel)
            .Concat(standaloneQuestChainEditorService.OpenedDocuments)
            .OfType<QuestChainDocumentViewModel>()
            .ToList();

        var store = new QuestStore();
        foreach (var editor in openedEditors)
        {
            var existingStore = await editor.BuildCurrentQuestStore();
            store.Merge(existingStore);
        }

        await questChainLoader.LoadChain(item.Entries.ToArray(), store);

        var query = await chainGenerator.Generate(store, item.ExistingData);
        var conditions = store.Where(x => x.Conditions.Count > 0)
            .ToDictionary(x => x.Entry, x => x.Conditions);
        return await queryGenerator.GenerateQuery(query.ToList(), item.ExistingData, conditions, item.ExistingConditions
            .ToDictionary(x => x.Key, x => (IReadOnlyList<ICondition>)x.Value));
    }
}