using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.LootEditor.Editor;
using WDE.LootEditor.Editor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Solution;

[AutoRegister]
public class LootSolutionItemProviders :
    ISolutionNameProvider<LootSolutionItem>,
    ISolutionItemIconProvider<LootSolutionItem>,
    ISolutionItemEditorProvider<LootSolutionItem>,
    ISolutionItemRemoteCommandProvider<LootSolutionItem>,
    ISolutionItemSerializer<LootSolutionItem>,
    ISolutionItemDeserializer<LootSolutionItem>
{
    private readonly IContainerProvider containerProvider;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ILootEditorFeatures lootEditorFeatures;

    public LootSolutionItemProviders(IContainerProvider containerProvider, 
        ICachedDatabaseProvider databaseProvider,
        ILootEditorFeatures lootEditorFeatures)
    {
        this.containerProvider = containerProvider;
        this.databaseProvider = databaseProvider;
        this.lootEditorFeatures = lootEditorFeatures;
    }
    
    public ImageUri GetIcon(LootSolutionItem icon)
    {
        return new ImageUri("Icons/document_loot_big.png");
    }

    public IDocument GetEditor(LootSolutionItem item)
    {
        var doc = containerProvider.Resolve<LootEditorViewModel>((typeof(LootSolutionItem), item));
        doc.BeginLoad().ListenErrors();
        return doc;
    }

    public IRemoteCommand[] GenerateCommand(LootSolutionItem item)
    {
        return new IRemoteCommand[] { new AnonymousRemoteCommand($"reload {lootEditorFeatures.GetTableNameFor(item.Type)}") };
    }

    public string GetName(LootSolutionItem item)
    {
        if (item.Type == LootSourceType.Creature)
        {
            var template = databaseProvider.GetCachedCreatureTemplate(item.Entry);
            if (template != null)
                return template.Name + " loot";
            return "Creature " + item.Entry + " loot";
        }
        else if (item.Type == LootSourceType.GameObject)
        {
            var template = databaseProvider.GetCachedGameObjectTemplate(item.Entry);
            if (template != null)
                return template.Name + " loot";
            return "GameObject " + item.Entry + " loot";
        }
        else
            return item.Type.ToString() + " " + item.Entry + " loot";
    }

    public ISmartScriptProjectItem Serialize(LootSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem()
        {
            Type = 44,
            Value = (int)item.Entry,
            Value2 = (int)item.Type,
            Comment = item.Difficulty.ToString()
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        if (projectItem.Type != 44 || !projectItem.Value2.HasValue)
        {
            solutionItem = null;
            return false;
        }

        uint.TryParse(projectItem.Comment, out var difficulty);

        solutionItem = new LootSolutionItem((LootSourceType)projectItem.Value2.Value, (uint)projectItem.Value, difficulty);
        return true;
    }
}