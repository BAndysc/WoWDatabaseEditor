using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.LootEditor.Services;

[SingleInstance]
[AutoRegister]
internal class LootEditorPreferences : ILootEditorPreferences
{
    private readonly IUserSettings settings;
    private Data data;

    private static List<LootButtonDefinition> DefaultButtons = new()
    {
        new()
        {
            ButtonToolTip = "Add a new item",
            ButtonType = LootButtonType.AddItem,
            Icon = "Icons/icon_add.png"
        },
        new()
        {
            ButtonToolTip = "Add a new currency",
            ButtonType = LootButtonType.AddCurrency,
            Icon = "Icons/icon_money.png"
        },
        new()
        {
            ButtonToolTip = "Add a new loot reference",
            ButtonType = LootButtonType.AddReference,
            Icon = "Icons/icon_hash.png"
        },
        new()
        {
            ButtonToolTip = "Import quest items from wowhead",
            ButtonType = LootButtonType.ImportQuestItemsFromWowHead,
            Icon = "Icons/icon_head_red_quest.png"
        },
        new()
        {
            ButtonToolTip = "Import items from wowhead",
            ButtonType = LootButtonType.ImportFromWowHead,
            Icon = "Icons/icon_head_red.png"
        }
    };

    static LootEditorPreferences()
    {
        DefaultButtons.Reverse();
    }
    
    public LootEditorPreferences(IUserSettings settings)
    {
        this.settings = settings;
        data = settings.Get<Data>();
        data.ColumnWidths ??= new();
        data.Buttons ??= new();

        bool HasButtonType(LootButtonType type)
        {
            return data.Buttons.Any(x => x.ButtonType == type);
        }

        foreach (var def in DefaultButtons)
        {
            if (!HasButtonType(def.ButtonType))
                data.Buttons.Insert(0, def);
        }

        buttons.Value = data.Buttons;
    }

    private struct Data : ISettings
    {
        public Dictionary<string, double> ColumnWidths { get; set; }
        public List<LootButtonDefinition> Buttons { get; set; }
    }
    
    public bool TryGetColumnWidth(string id, out double width)
    {
        return data.ColumnWidths.TryGetValue(id, out width);
    }

    public void SaveColumnsWidth(IEnumerable<(string, double)> widths)
    {
        data.ColumnWidths.Clear();
        foreach (var (id, width) in widths)
            data.ColumnWidths[id] = width;
        settings.Update(data);
    }

    public void SaveButtons(IEnumerable<LootButtonDefinition> buttons)
    {
        this.buttons.Value = data.Buttons = buttons.ToList();
        settings.Update(data);
    }

    private ReactiveProperty<IEnumerable<LootButtonDefinition>> buttons = new(new List<LootButtonDefinition>());
    
    public IReadOnlyReactiveProperty<IEnumerable<LootButtonDefinition>> Buttons => buttons;
}