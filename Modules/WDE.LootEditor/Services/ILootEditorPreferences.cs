using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.LootEditor.Services;

[UniqueProvider]
internal interface ILootEditorPreferences
{
    bool TryGetColumnWidth(string id, out double width);
    void SaveColumnsWidth(IEnumerable<(string, double)> widths);
    void SaveButtons(IEnumerable<LootButtonDefinition> buttons);
    IReadOnlyReactiveProperty<IEnumerable<LootButtonDefinition>> Buttons { get; }
}

public struct LootButtonDefinition
{
    public LootButtonType ButtonType { get; set; }
    public string? Icon { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonToolTip { get; set; }
    public List<CustomLootItemDefinition>? CustomItems { get; set; }
    
    public struct CustomLootItemDefinition
    {
        public int ItemOrCurrencyId { get; set; }
        public float Chance { get; set; }
        public int LootMode { get; set; }
        public int GroupId { get; set; }
        public int MinCountOrRef { get; set; }
        public int MaxCount { get; set; }
        public int BadLuckProtectionId { get; set; }
        public string? Comment { get; set; }
        public uint ConditionId { get; set; }
        public List<AbstractCondition>? Conditions { get; set; }
    }
}

public enum LootButtonType
{
    AddItemSet,
    AddItem,
    AddCurrency,
    AddReference,
    ImportQuestItemsFromWowHead,
    ImportFromWowHead
}