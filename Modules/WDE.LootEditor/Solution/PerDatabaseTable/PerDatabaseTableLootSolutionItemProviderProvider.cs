using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Types;
using WDE.LootEditor.Editor;
using WDE.LootEditor.Utils;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Solution.PerDatabaseTable;

[AutoRegister]
public class PerDatabaseTableLootSolutionItemProviderProvider : ISolutionItemProviderProvider
{
    private readonly ILootEditorFeatures features;
    private readonly IParameterPickerService parameterPickerService;

    private class PerDatabaseTableLootSolutionItemProvider : ISolutionItemProvider, IRawDatabaseTableSolutionItemProvider
    {
        private readonly IParameterPickerService parameterPickerService;
        private readonly string parameterName;
        private readonly LootSourceType type;

        public PerDatabaseTableLootSolutionItemProvider(IParameterPickerService parameterPickerService,
            string parameterName,
            LootSourceType type)
        {
            this.parameterPickerService = parameterPickerService;
            this.parameterName = parameterName;
            this.type = type;
        }

        public string GetName() => $"{type} loot editor";

        public ImageUri GetImage() => new("Icons/document_loot.png");

        public string GetDescription() => $"Allows editing loot for {type}.";

        public string GetGroupName() => "Loot";

        public bool IsCompatibleWithCore(ICoreVersion core) => core.LootEditingMode == LootEditingMode.PerDatabaseTable;

        public bool ByDefaultHideFromQuickStart => true;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            return new PerDatabaseTableLootSolutionItem(type);
        }

        public string TableName => $"{type.ToString().ToLower()}_loot_template";
    }

    public PerDatabaseTableLootSolutionItemProviderProvider(ILootEditorFeatures features,
        IParameterPickerService parameterPickerService)
    {
        this.features = features;
        this.parameterPickerService = parameterPickerService;
    }
    
    public IEnumerable<ISolutionItemProvider> Provide()
    {
        foreach (var supportedType in features.SupportedTypes)
        {
            if (supportedType is LootSourceType.Disenchant)
                continue; // need to research how to implement them (i.e. for disenchant we need to know DisenchantID from the dbc)
            
            yield return new PerDatabaseTableLootSolutionItemProvider(parameterPickerService,
                supportedType.GetParameterFor(),
                supportedType);
        }
    }
}