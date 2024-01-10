using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AvaloniaStyles.Controls.FastTableView;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.LootEditor.Editor.ViewModels;
using WDE.LootEditor.Services;
using WDE.MVVM;

namespace WDE.LootEditor.Configuration;

public partial class CustomLootItemSettingViewModel : ObservableBase, ITableRow
{
    [Notify] private long itemOrCurrencyId;
    [Notify] private float chance = 100;
    [Notify] private long lootMode = 1;
    [Notify] private long groupId;
    [Notify] private long minCountOrRef = 1;
    [Notify] private long maxCount = 1;
    [Notify] private long badLuckProtectionId = 0;
    [Notify] private IReadOnlyList<ICondition>? conditions;
    [Notify] private long conditionId;
    [Notify] private string? comment;

    private IParameter<long> itemCurrencyParameter;
    
    private List<ITableCell> cells = new List<ITableCell>();

    public string ItemOrCurrencyName => minCountOrRef > 0 ? itemCurrencyParameter.ToString(itemOrCurrencyId, ToStringOptions.WithoutNumber) : "";

    public CustomLootItemSettingViewModel(LootEditorButtonSettingViewModel parent)
    {
        itemCurrencyParameter = parent.Parent.ParameterFactory.Factory("ItemCurrencyParameter");
        cells.Add(new TableCell<long>(this, itemCurrencyParameter, () => ItemOrCurrencyId, x => ItemOrCurrencyId = x));
        cells.Add(new ItemNameCell(this));
        cells.Add(new TableCell<float>(this, FloatParameter.Instance, () => Chance, x => Chance = x));
        cells.Add(new TableCell<long>(this, parent.Parent.ParameterFactory.Factory("LootModeParameter"), () => LootMode, x => LootMode = x));
        cells.Add(new TableCell<long>(this, Parameter.Instance, () => GroupId, x => GroupId = x));
        cells.Add(new TableCell<long>(this, Parameter.Instance, () => MinCountOrRef, x => MinCountOrRef = x));
        cells.Add(new TableCell<long>(this, Parameter.Instance, () => MaxCount, x => MaxCount = x));
        if (parent.Parent.EditorFeatures.HasConditionId)
            cells.Add(new TableCell<long>(this, Parameter.Instance, () => ConditionId, x => ConditionId = x));
        else
            cells.Add(new ActionCell(parent.Parent.EditConditionsCommand, this, () => $"Conditions ({conditions?.CountActualConditions() ?? 0})"));
        if (parent.Parent.EditorFeatures.HasBadLuckProtectionId)
            cells.Add(new TableCell<long>(this, Parameter.Instance, () => BadLuckProtectionId, x => BadLuckProtectionId = x));
        cells.Add(new TableCell<string>(this, StringParameter.Instance, () => Comment ?? "", x => Comment = x));

        this.PropertyChanged += (_, __) =>
        {
            Changed?.Invoke(this);
        };
    }

    public CustomLootItemSettingViewModel(LootEditorButtonSettingViewModel parent,
        LootButtonDefinition.CustomLootItemDefinition def)
        : this(parent)
    {
        itemOrCurrencyId = def.ItemOrCurrencyId;
        chance = def.Chance;
        lootMode = def.LootMode;
        groupId = def.GroupId;
        minCountOrRef = def.MinCountOrRef;
        maxCount = def.MaxCount;
        badLuckProtectionId = def.BadLuckProtectionId;
        conditionId = def.ConditionId;
        comment = def.Comment;
        if (def.Conditions != null)
            conditions = def.Conditions;
    }

    public IReadOnlyList<ITableCell> CellsList => cells;
    public event Action<ITableRow>? Changed;

    public class ItemNameCell : ITableCell
    {
        public readonly CustomLootItemSettingViewModel Row;
        
        public ItemNameCell(CustomLootItemSettingViewModel row)
        {
            this.Row = row;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        public void UpdateFromString(string newValue) { }

        public string? StringValue => Row.ItemOrCurrencyName;
        public long Item => Row.itemOrCurrencyId;

        public override string? ToString() => StringValue;
    }
    
    public class TableCell<T> : ITableCell where T : notnull
    {
        private readonly CustomLootItemSettingViewModel vm;
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        public readonly IParameter<T> Parameter;
        public event PropertyChangedEventHandler? PropertyChanged;

        public TableCell(CustomLootItemSettingViewModel vm,
            IParameter<T> parameter,
            Func<T> getter,
            Action<T> setter)
        {
            this.vm = vm;
            this.Parameter = parameter;
            this.getter = getter;
            this.setter = setter;
        }

        public T Value => getter();
        
        public void UpdateFromString(string newValue)
        {
            if (typeof(T) == typeof(long))
            {
                var asLong = Convert.ToInt64(newValue);
                setter((T)(object)asLong);
            }
            else if (typeof(T) == typeof(float))
            {
                var asFloat = Convert.ToSingle(newValue);
                setter((T)(object)asFloat);
            }
            else if (typeof(T) == typeof(string))
            {
                setter((T)(object)newValue);
            }
            else
                throw new Exception($"Type {typeof(T)} not supported");
        }

        public string? StringValue => getter()?.ToString();

        public override string? ToString()
        {
            return StringValue;
        }
    }

    public LootButtonDefinition.CustomLootItemDefinition ToDefinition()
    {
        return new LootButtonDefinition.CustomLootItemDefinition()
        {
            ItemOrCurrencyId = (int)itemOrCurrencyId,
            Chance = chance,
            LootMode = (int)lootMode,
            GroupId = (int)groupId,
            MinCountOrRef = (int)minCountOrRef,
            MaxCount = (int)maxCount,
            BadLuckProtectionId = (int)badLuckProtectionId,
            Conditions = conditions?.Select(x => new AbstractCondition(x)).ToList(),
            ConditionId = (uint)conditionId,
            Comment = comment
        };
    }
}