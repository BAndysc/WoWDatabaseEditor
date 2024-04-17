using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.LootEditor.Editor;
using WDE.LootEditor.Editor.Standalone;
using WDE.LootEditor.Editor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Solution.PerDatabaseTable;

[AutoRegister]
public class PerDatabaseTableLootSolutionItemProviders :
    ISolutionNameProvider<PerDatabaseTableLootSolutionItem>,
    ISolutionItemIconProvider<PerDatabaseTableLootSolutionItem>,
    ISolutionItemEditorProvider<PerDatabaseTableLootSolutionItem>,
    ISolutionItemRemoteCommandProvider<PerDatabaseTableLootSolutionItem>,
    ISolutionItemSerializer<PerDatabaseTableLootSolutionItem>,
    ISolutionItemDeserializer<PerDatabaseTableLootSolutionItem>
{
    private readonly IContainerProvider containerProvider;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ILootEditorFeatures lootEditorFeatures;

    public PerDatabaseTableLootSolutionItemProviders(IContainerProvider containerProvider, 
        ICachedDatabaseProvider databaseProvider,
        ILootEditorFeatures lootEditorFeatures)
    {
        this.containerProvider = containerProvider;
        this.databaseProvider = databaseProvider;
        this.lootEditorFeatures = lootEditorFeatures;
    }
    
    public ImageUri GetIcon(PerDatabaseTableLootSolutionItem icon)
    {
        return new ImageUri("Icons/document_loot_big.png");
    }

    public IDocument GetEditor(PerDatabaseTableLootSolutionItem item)
    {
        var doc = containerProvider.Resolve<StandaloneLootEditorViewModel>((typeof(PerDatabaseTableLootSolutionItem), item));
        doc.CanChangeLootType = false;
        return doc;
    }

    public IRemoteCommand[] GenerateCommand(PerDatabaseTableLootSolutionItem item)
    {
        return new IRemoteCommand[] { new AnonymousRemoteCommand($"reload {lootEditorFeatures.GetTableNameFor(item.Type)}") };
    }

    public string GetName(PerDatabaseTableLootSolutionItem item)
    {
        return item.Type + " loot editor";
    }

    public ISmartScriptProjectItem? Serialize(PerDatabaseTableLootSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem()
        {
            Type = 46,
            Value = 0,
            Value2 = (int)item.Type,
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        if (projectItem.Type != 46 || !projectItem.Value2.HasValue)
        {
            solutionItem = null;
            return false;
        }

        solutionItem = new PerDatabaseTableLootSolutionItem((LootSourceType)projectItem.Value2.Value);
        return true;
    }
}