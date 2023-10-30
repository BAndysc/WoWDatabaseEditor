using System;
using System.Collections.Generic;
using System.ComponentModel;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Utils;

namespace WDE.LootEditor.Picker.ViewModels;

// todo: refactor to a struct, but then I can't use VeryFastTableView
public class ItemViewModel : ITableRow
{
    public int ItemOrCurrencyId { get; }
    public string Name { get; }
    public float Chance { get; }
    public uint LootMode { get; }
    public int GroupId { get; }
    public int MinCountOrRef { get; }
    public uint MaxCount { get; }
    
    public bool IsReference => MinCountOrRef < 0;
    
    public uint ReferenceId => IsReference ? (uint)-MinCountOrRef : 0;

    public ItemViewModel(LootPickerViewModel parent, ILootEntry entry, ILootTemplateName? referenceName)
    {
        ItemOrCurrencyId = entry.ItemOrCurrencyId;
        Chance = entry.Chance;
        LootMode = entry.LootMode;
        GroupId = entry.GroupId;
        MinCountOrRef = entry.IsReference() ? -(int)entry.Reference : entry.MinCount;
        MaxCount = entry.MaxCount;

        Name = "";
        if (!IsReference)
        {
            if (ItemOrCurrencyId < 0)
            {
                if (parent.ItemStore.GetCurrencyTypeById((uint)-ItemOrCurrencyId) is { } currency)
                    Name = currency.Name;
                else
                    Name = "Unknown currency";
            }
            else
                Name = parent.ItemParameter.ToString(ItemOrCurrencyId, ToStringOptions.WithoutNumber);
        }
        else
        {
            if (referenceName != null)
                Name = referenceName.Name;
        }
        
        CellsList = new List<ITableCell>()
        {
            new IntCell(() => ItemOrCurrencyId, _ => { }),
            new ItemNameCell(this),
            new FloatCell(() => Chance, _ => {}),
            new IntCell(() => (int)LootMode, _ => {}),
            new IntCell(() => GroupId, _ => {}),
            new IntCell(() => MinCountOrRef, _ => {}),
            new IntCell(() => (int)MaxCount, _ => {}),
        };
    }

    public class ItemNameCell : ITableCell
    {
        public readonly ItemViewModel Item;

        public ItemNameCell(ItemViewModel item)
        {
            this.Item = item;
        }
        public void UpdateFromString(string newValue) { }
        public string StringValue => Item.Name;
        public override string ToString() => StringValue;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    
    public IReadOnlyList<ITableCell> CellsList { get; }
    public event Action<ITableRow>? Changed;

    public bool Search(string search)
    {
        return ItemOrCurrencyId.Contains(search) ||
               Name.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}