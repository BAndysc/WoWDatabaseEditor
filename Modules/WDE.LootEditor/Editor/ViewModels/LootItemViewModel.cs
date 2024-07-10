using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using DynamicData;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.LootEditor.Models;
using WDE.MVVM;

namespace WDE.LootEditor.Editor.ViewModels;

public class OnBeforeChangedEventArgs<T> : EventArgs
{
    public OnBeforeChangedEventArgs(T old, T @new)
    {
        Old = old;
        New = @new;
    }

    public T Old { get; }
    public T New { get; }
    public bool Cancel { get; set; }
}

public partial class LootItemViewModel : ObservableBase, ITableRow
{
    public LootGroup Parent { get; }
    public LootEditorViewModel ParentVm => Parent.ParentVm;
    
    public LootEntry Entry => Parent.LootEntry;
    
    internal LootItemParameterCell<long> ItemOrCurrencyId { get; }
    internal LootItemParameterCell<float> Chance { get; }
    internal LootItemParameterCell<long> LootMode { get; }
    internal LootItemParameterCell<long> GroupId { get; }
    internal LootItemParameterCell<long> MinCountOrRef { get; }
    internal LootItemParameterCell<long> MaxCount { get; }
    internal LootItemParameterCell<long> BadLuckProtectionId { get; }
    internal LootItemParameterCell<string> Comment { get; }
    internal LootItemParameterCell<long> Build { get; }
    internal LootItemParameterCell<long> MinPatch { get; }
    internal LootItemParameterCell<long> MaxPatch { get; }
    internal LootItemParameterCell<long> ConditionId { get; }
    
    private IReadOnlyList<ICondition>? conditions;
    public IReadOnlyList<ICondition>? Conditions
    {
        get => conditions;
        set => SetPropertyWithOldValue(ref conditions, value);
    }

    public bool IsReference => MinCountOrRef.Value < 0;
   
    public uint ReferenceEntry
    {
        get
        {
            Debug.Assert(IsReference);
            return (uint)-MinCountOrRef.Value;
        }
    }

    [Notify] private bool isModified;
    [Notify] private bool isDuplicate;

    public event Func<LootItemViewModel, EventArgs, Task>? OnReferenceLootChanged;
    public event Func<LootItemViewModel, OnBeforeChangedEventArgs<long>, Task>? OnBeforeReferenceLootChanged;

    private string? cachedName = "";
    
    public LootItemViewModel(LootGroup group)
    {
        Parent = group;
        ItemOrCurrencyId = new LootItemParameterCellLong(this, ParentVm.ParameterFactory.Factory("ItemCurrencyParameter"), true);
        Chance = new LootItemParameterCell<float>(this, FloatParameter.Instance);
        LootMode = new LootItemParameterCellLong(this, ParentVm.ParameterFactory.Factory("LootModeParameter"));
        GroupId = new LootItemParameterCellLong(this, Parameter.Instance, zeroIsBlank: true);
        MinCountOrRef = new LootItemParameterCellLong(this, ParentVm.ParameterFactory.Factory("MinValueOrLootReferenceParameter"));
        BadLuckProtectionId = new LootItemParameterCellLong(this, ParentVm.ParameterFactory.Factory("BadLuckProtectionTemplateParameter"), zeroIsBlank: true);
        MaxCount = new LootItemParameterCellLong(this, Parameter.Instance, zeroIsBlank: true);
        Comment = new LootItemParameterCell<string>(this, StringParameter.Instance);
        Build = new LootItemParameterCellLong(this, Parameter.Instance);
        MinPatch = new LootItemParameterCellLong(this, Parameter.Instance);
        MaxPatch = new LootItemParameterCellLong(this, Parameter.Instance);
        ConditionId = new LootItemParameterCellLong(this, Parameter.Instance, zeroIsBlank: true);

        // defaults
        LootMode.SetValueFromHistory(1);
        Chance.SetValueFromHistory(100);
        MinCountOrRef.SetValueFromHistory(1);
        MaxCount.SetValueFromHistory(1);

        var cells = new List<ITableCell>
        {
            ItemOrCurrencyId,
            new ItemNameStringCell(this, () =>
            {
                cachedName ??= GetItemOrCurrencyName();
                return cachedName;
            }, () => (int)ItemOrCurrencyId.Value),
            Chance
        };
        
        if (ParentVm.LootEditorFeatures.HasLootModeField)
            cells.Add(LootMode);
        
        cells.Add(GroupId);
        cells.Add(MinCountOrRef);
        cells.Add(MaxCount);
        
        if (ParentVm.LootEditorFeatures.HasBadLuckProtectionId)
            cells.Add(BadLuckProtectionId);
    
        if (ParentVm.LootEditorFeatures.HasConditionId)
            cells.Add(ConditionId);
        else
            cells.Add(new ActionCell(ParentVm.EditConditionsCommand, this, () => $"Conditions ({conditions.CountActualConditions()})"));

        if (ParentVm.LootEditorFeatures.HasCommentField(ParentVm.LootSourceType))
            cells.Add(Comment);
        
        if (ParentVm.LootEditorFeatures.HasPatchField)
        {
            cells.Add(MinPatch);
            cells.Add(MaxPatch);
        }
        CellsList = cells;
        
        foreach (var c in CellsList)
        {
            c.PropertyChanged += (sender, e) =>
            {
                Changed?.Invoke(this);
                IsModified = true;
            };
        }

        MinCountOrRef.OnBeforeChanged += (sender, e) => OnBeforeReferenceLootChanged.Raise(this, e);
        MinCountOrRef.OnChangeAsync += (sender, e) => OnReferenceLootChanged.Raise(this, EventArgs.Empty);
        foreach (var cell in CellsList
                     .Select(x => x as INotifyPropertyValueChanged)
                     .Where(x => x != null)
                     .Cast<INotifyPropertyValueChanged>())
        {
            cell.PropertyValueChanged += (sender, e) =>
            {
                OnHistoryAction?.Invoke(new AnonymousHistoryAction("Change " + e.PropertyName, () =>
                {
                    if (sender is LootItemParameterCell<long> longCell)
                        longCell.SetValueFromHistory((long)e.Old!);
                    else if (sender is LootItemParameterCell<string> stringCell)
                        stringCell.SetValueFromHistory((string)e.Old!);
                }, () =>
                {
                    if (sender is LootItemParameterCell<long> longCell)
                        longCell.SetValueFromHistory((long)e.New!);
                    else if (sender is LootItemParameterCell<string> stringCell)
                        stringCell.SetValueFromHistory((string)e.New!);
                }));
            };
        }

        ItemOrCurrencyId.PropertyChanged += (_, _) => InvalidateName();
        MinCountOrRef.PropertyChanged += (_, _) => InvalidateName();
    }
    
    public LootItemViewModel(LootGroup parent, LootModel loot)
        : this(parent)
    {
        ItemOrCurrencyId.SetValueFromHistory(loot.Loot.ItemOrCurrencyId);
        Chance.SetValueFromHistory(Math.Abs(loot.Loot.Chance) * (loot.Loot.QuestRequired ? -1 : 1));
        LootMode.SetValueFromHistory(loot.Loot.LootMode);
        GroupId.SetValueFromHistory(loot.Loot.GroupId);
        MinCountOrRef.SetValueFromHistory(loot.Loot.IsReference() ? -(int)loot.Loot.Reference : loot.Loot.MinCount);
        MaxCount.SetValueFromHistory(loot.Loot.MaxCount);
        BadLuckProtectionId.SetValueFromHistory(loot.Loot.BadLuckProtectionId);
        Build.SetValueFromHistory(loot.Loot.Build);
        Comment.SetValueFromHistory(loot.Loot.Comment ?? "");
        MinPatch.SetValueFromHistory(loot.Loot.MinPatch);
        MaxPatch.SetValueFromHistory(loot.Loot.MaxPatch);
        ConditionId.SetValueFromHistory(loot.Loot.ConditionId);
        Conditions = loot.Conditions;
        SetAsSaved();
    }

    public string GetItemOrCurrencyName()
    {
        if (IsReference)
        {
            var refGroup = ParentVm.Loots.FirstOrDefault(x => x.LootSourceType == LootSourceType.Reference && (uint)x.LootEntry == ReferenceEntry);

            if (refGroup == null)
            {
                if (ParentVm.TryGetLootName(LootSourceType.Reference, new LootEntry(ReferenceEntry), out var name))
                    return name!;
            }
            
            return refGroup?.GroupName ?? "";
        }
        
        if (ItemOrCurrencyId.Value < 0)
        {
            if (ParentVm.ItemStore.GetCurrencyTypeById((uint)-ItemOrCurrencyId.Value) is { } currencyType)
                return currencyType.Name;
            return "(unknown currency)";
        }
        else
        {
            if (ParentVm.ItemStore.GetItemSparseById((int)ItemOrCurrencyId.Value) is { } itemSparse)
                return itemSparse.Name;
            
            if (ParentVm.ItemParameter.Items != null &&
                ParentVm.ItemParameter.Items.TryGetValue((int)ItemOrCurrencyId.Value, out var item))
                return item.Name;
            
            return "(unknown item)";
        }
    }

    public void SetAsSaved()
    {
        IsModified = false;
    }
    
    public event Action<IHistoryAction>? OnHistoryAction;

    public IReadOnlyList<ITableCell> CellsList { get; set; }
    
    public event Action<ITableRow>? Changed;

    public void InvalidateName()
    {
        cachedName = null;
        Changed?.Invoke(this);
    }

    public LootModel ToModel()
    {
        return new LootModel(new AbstractLootEntry()
        {
            SourceType = Parent.LootSourceType,
            Entry = (uint)Parent.LootEntry,
            ItemOrCurrencyId = (int)ItemOrCurrencyId.Value,
            Chance = Math.Abs(Chance.Value),
            QuestRequired = Chance.Value < 0,
            LootMode = (uint)LootMode.Value,
            GroupId = (ushort)GroupId.Value,
            Reference = IsReference ? ReferenceEntry : 0,
            MinCount = IsReference ? 1 : (int)MinCountOrRef.Value,
            MaxCount = (uint)MaxCount.Value,
            BadLuckProtectionId = (int)BadLuckProtectionId.Value,
            MinPatch = (ushort)MinPatch.Value,
            MaxPatch = (ushort)MaxPatch.Value,
            ConditionId = (uint)ConditionId.Value,
            Comment = Comment.Value,
            Build = (uint)Build.Value,
        }, Conditions?.Select(x => new AbstractCondition(x)).ToList());
    }
}

internal class ItemNameStringCell : StringCell
{
    public Func<int> ItemGetter { get; }

    public ItemNameStringCell(LootItemViewModel vm,
        Func<string?> getter, Func<int> itemGetter) : base(getter, _ => {})
    {
        ViewModel = vm;
        ItemGetter = itemGetter;
    }

    public int Item => ItemGetter();
    public LootItemViewModel ViewModel { get; }
}


internal class LootItemParameterCellLong : LootItemParameterCell<long>
{
    private readonly bool zeroIsBlank;

    public LootItemParameterCellLong(LootItemViewModel parent,
        IParameter<long> parameter, 
        bool displayRawValue = false,
        bool zeroIsBlank = false) : base(parent, parameter, displayRawValue)
    {
        this.zeroIsBlank = zeroIsBlank;
    }

    public override string? ToString()
    {
        if (Value == 0 && zeroIsBlank)
            return "";
        return base.ToString();
    }
}

internal class LootItemParameterCell<T> : ITableCell, INotifyPropertyValueChanged where T : notnull
{
    public LootItemViewModel Parent { get; }
    private readonly IParameter<T> parameter;
    private readonly bool displayRawValue;
    public event PropertyChangedEventHandler? PropertyChanged;

    public IParameter<T> Parameter => parameter;

    public event Func<LootItemParameterCell<T>, OnBeforeChangedEventArgs<T>, Task>? OnBeforeChanged;
    public event Func<LootItemParameterCell<T>, EventArgs, Task>? OnChangeAsync;

    private string? toString;

    private T value;
    public T Value => value;

    private async Task EvaluateToStringAsync(IAsyncParameter<T> asyncParameter)
    {
        toString = await asyncParameter.ToStringAsync(value, default);
    }

    public LootItemParameterCell(LootItemViewModel parent, IParameter<T> parameter,
        bool displayRawValue = false)
    {
        Parent = parent;
        value = default!;
        toString = displayRawValue ? value.ToString()! : parameter.ToString(value);
        this.parameter = parameter;
        this.displayRawValue = displayRawValue;
    }

    private Task? previousUpdateTask;

    private async Task SetValueInternal(T newValue)
    {
        var oldValue = this.value;
        this.value = newValue;
        toString = displayRawValue ? value.ToString() : parameter.ToString(newValue);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        await OnChangeAsync.Raise(this, EventArgs.Empty);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
        PropertyValueChanged?.Invoke(this,
            new TypedPropertyValueChangedEventArgs<T>(nameof(Value), oldValue, newValue));
        if (parameter is IAsyncParameter<T> asyncParameter)
            await EvaluateToStringAsync(asyncParameter);
    }

    public void SetValueFromHistory(T newValue)
    {
        SetValueInternal(newValue).ListenErrors();
    }

    public async Task SetValue(T newValue)
    {
        async Task UpdateAsync(Task? taskToWaitFor)
        {
            if (taskToWaitFor != null && !taskToWaitFor.IsCompleted)
                await taskToWaitFor;

            var e = new OnBeforeChangedEventArgs<T>(value, newValue);
            await OnBeforeChanged.Raise(this, e);

            if (e.Cancel)
                return;

            using var _ = Parent.ParentVm.HistoryHandler.WithinBulk("Change value");
            await SetValueInternal(newValue);
        }

        previousUpdateTask = UpdateAsync(previousUpdateTask);
        await previousUpdateTask;
    }

    public Task UpdateFromStringAsync(string newValue)
    {
        try
        {
            var newValueTyped = (T)Convert.ChangeType(newValue, typeof(T));
            return SetValue(newValueTyped);
        }
        catch (FormatException)
        {
            return Task.CompletedTask;
        }
    }

    public void UpdateFromString(string newValue)
    {
        UpdateFromStringAsync(newValue).ListenErrors();
    }

    public string StringValue => value?.ToString() ?? "";

    public override string? ToString() => value == null ? "(null)" : toString;
    public event PropertyValueChangedEventHandler? PropertyValueChanged;
}

internal class ActionCell : ITableCell
{
    private readonly Func<string> text;

    public ActionCell(ICommand command, object commandParameter, Func<string> text)
    {
        this.text = text;
        Command = command;
        CommandParameter = commandParameter;
    }

    public ICommand Command { get; }
    public object CommandParameter { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void UpdateFromString(string newValue) { }
    public string StringValue => text();
}