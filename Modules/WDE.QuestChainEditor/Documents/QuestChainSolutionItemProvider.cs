using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Services;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Documents;

[AutoRegisterToParentScope]
[SingleInstance]
public class QuestChainSolutionItemProvider : ISolutionItemProvider,
    ISolutionItemEditorProvider<QuestChainSolutionItem>,
    ISolutionItemIconProvider<QuestChainSolutionItem>,
    ISolutionNameProvider<QuestChainSolutionItem>,
    ISolutionItemDeserializer<QuestChainSolutionItem>,
    ISolutionItemSerializer<QuestChainSolutionItem>,
    ISolutionItemRemoteCommandProvider<QuestChainSolutionItem>
{
    private readonly IContainerProvider containerProvider;
    private readonly IQuestEntryProviderService questEntryProviderService;
    private readonly IQuestChainEditorConfiguration configuration;

    public QuestChainSolutionItemProvider(IContainerProvider containerProvider,
        IQuestEntryProviderService questEntryProviderService,
        IQuestChainEditorConfiguration configuration)
    {
        this.containerProvider = containerProvider;
        this.questEntryProviderService = questEntryProviderService;
        this.configuration = configuration;
    }

    public string GetName() => "Quest chain";

    public ImageUri GetImage() => new ImageUri("Icons/document_quest_chain_big.png");

    public string GetDescription() => "Allows editing quest chains";

    public string GetGroupName() => "Quests";

    public bool IsCompatibleWithCore(ICoreVersion core) => true;

    public async Task<ISolutionItem?> CreateSolutionItem()
    {
        var quest = await questEntryProviderService.GetEntryFromService();
        if (quest.HasValue)
        {
            var item = new QuestChainSolutionItem();
            item.AddEntry(quest.Value);
            return item;
        }

        return null;
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
        if (projectItem.Type == 48)
        {
            if (projectItem.Comment != null)
            {
                try
                {
                    solutionItem = JsonConvert.DeserializeObject<QuestChainSolutionItem>(projectItem.Comment);
                    return true;
                }
                catch (Exception e)
                {
                    LOG.LogError("Couldn't deserialize quest chain: " + projectItem.Comment, e);
                }
            }
            var entries = projectItem.StringValue?.Split(',').Where(x => uint.TryParse(x, out var _ )).Select(uint.Parse).ToList();
            var chainSolutionItem = new QuestChainSolutionItem();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    chainSolutionItem.AddEntry(entry);
                }
            }

            solutionItem = chainSolutionItem;
            return true;
        }

        solutionItem = null;
        return false;
    }

    public ISmartScriptProjectItem? Serialize(QuestChainSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem()
        {
            Type = 48,
            StringValue = string.Join(",", item.Entries),
            Comment = forMostRecentlyUsed ? null : SerializeExistingDataAndConditions(item)
        };
    }

    private string? SerializeExistingDataAndConditions(QuestChainSolutionItem item)
    {
        return JsonConvert.SerializeObject(item);
    }

    public IRemoteCommand[] GenerateCommand(QuestChainSolutionItem item)
    {
        if (configuration.ShowMarkConditionSourceType.HasValue)
        {
            return new IRemoteCommand[]
            {
                new AnonymousRemoteCommand("reload quest_template"),
                new AnonymousRemoteCommand("reload conditions " + configuration.ShowMarkConditionSourceType)
            };
        }
        else
        {
            return new IRemoteCommand[]
            {
                new AnonymousRemoteCommand("reload quest_template")
            };
        }
    }
}