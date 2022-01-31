using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Documents;

[AutoRegister]
[SingleInstance]
public class QuestChainSolutionItemProvider : ISolutionItemProvider,
    ISolutionItemEditorProvider<QuestChainSolutionItem>,
    ISolutionItemIconProvider<QuestChainSolutionItem>,
    ISolutionNameProvider<QuestChainSolutionItem>,
    ISolutionItemDeserializer<QuestChainSolutionItem>,
    ISolutionItemSerializer<QuestChainSolutionItem>
{
    private readonly IContainerProvider containerProvider;
    private readonly IDatabaseProvider databaseProvider;
    private readonly ICreatureEntryOrGuidProviderService creatureEntryOrGuidProviderService;

    public QuestChainSolutionItemProvider(IContainerProvider containerProvider,
        IDatabaseProvider databaseProvider,
        ICreatureEntryOrGuidProviderService creatureEntryOrGuidProviderService)
    {
        this.containerProvider = containerProvider;
        this.databaseProvider = databaseProvider;
        this.creatureEntryOrGuidProviderService = creatureEntryOrGuidProviderService;
    }

    public string GetName() => "Quest chain";

    public ImageUri GetImage() => new ImageUri("Icons/document_quest_chain_big.png");

    public string GetDescription() => "Allows editing quest chains";

    public string GetGroupName() => "Quests";

    public bool IsCompatibleWithCore(ICoreVersion core) => true;

    public async Task<ISolutionItem?> CreateSolutionItem()
    {
        return new QuestChainSolutionItem();
    }

    public IDocument GetEditor(QuestChainSolutionItem item)
    {
        return containerProvider.Resolve<QuestChainDocumentViewModel>((typeof(QuestChainSolutionItem),
            item));
    }

    public ImageUri GetIcon(QuestChainSolutionItem icon) => new ImageUri("Icons/document_quest_chain.png");

    public string GetName(QuestChainSolutionItem item)
    {
        return "Quest chain";
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        if (projectItem.Type == 43)
        {
            solutionItem = new QuestChainSolutionItem();
            return true;
        }

        solutionItem = null;
        return false;
    }

    public ISmartScriptProjectItem Serialize(QuestChainSolutionItem item)
    {
        return new AbstractSmartScriptProjectItem()
        {
            Type = 43
        };
    }
}