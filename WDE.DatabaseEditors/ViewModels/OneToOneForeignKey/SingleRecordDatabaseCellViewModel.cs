using System;
using System.Reactive.Linq;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters;

namespace WDE.DatabaseEditors.ViewModels.OneToOneForeignKey;

public class SingleRecordDatabaseCellViewModel : BaseDatabaseCellViewModel, ITableCell
{
    public DatabaseEntityViewModel Parent { get; }
    public IDatabaseField? TableField { get; }
    public bool IsVisible { get; private set; } = true;
    public string? OriginalValueTooltip { get; private set; }
    public bool CanBeNull { get; }
    public bool IsReadOnly { get; }
    public bool CanBeSetToNull => CanBeNull && !IsReadOnly;
    public bool CanBeReverted => !IsReadOnly;
    public int ColumnIndex { get; }
    public ICommand? ActionCommand { get; }
    public string ActionLabel { get; set; } = "";
    
    public string ColumnName { get; }
    public bool IsBool => ParameterValue is IParameterValue<long> holder2 && holder2.Parameter is BoolParameter;
    public bool IsModified => TableField?.IsModified ?? false;
    
    public SingleRecordDatabaseCellViewModel(int columnIndex, DatabaseColumnJson columnDefinition, DatabaseEntityViewModel parent, DatabaseEntity parentEntity, IDatabaseField tableField, IParameterValue parameterValue) : base(parentEntity)
    {
        ColumnIndex = columnIndex * 2;
        CanBeNull = columnDefinition.CanBeNull;
        IsReadOnly = columnDefinition.IsReadOnly;
        ColumnName = columnDefinition.Name;
        Parent = parent;
        TableField = tableField;
        ParameterValue = parameterValue;

        if (UseItemPicker)
        {
            AutoDispose(ParameterValue.ToObservable().Subscribe(_ => RaisePropertyChanged(nameof(OptionValue))));
        }

        if (UseFlagsPicker)
        {
            AutoDispose(ParameterValue.ToObservable().Subscribe(_ => RaisePropertyChanged(nameof(AsLongValue))));
        }

        OriginalValueTooltip =
            tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
        AutoDispose(parameterValue.ToObservable("Value").Skip(1).SubscribeAction(_ =>
        {
            Parent.RaiseChanged(this, tableField.FieldName);
            OriginalValueTooltip =
                tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
            RaisePropertyChanged(nameof(OriginalValueTooltip));
            RaisePropertyChanged(nameof(AsBoolValue));
            RaisePropertyChanged(nameof(IsModified));
        }));
    }

    public SingleRecordDatabaseCellViewModel(int columnIndex, string columnName, ICommand action, DatabaseEntityViewModel parent, DatabaseEntity entity, string label) : base(entity)
    {
        Parent = parent;
        ColumnIndex = columnIndex * 2;
        CanBeNull = false;
        IsReadOnly = false;
        ColumnName = columnName;
        OriginalValueTooltip = null;
        ActionCommand = action;
        ActionLabel = label;
    }
    
    public SingleRecordDatabaseCellViewModel(int columnIndex, string columnName, ICommand action, DatabaseEntityViewModel parent, DatabaseEntity entity, System.IObservable<string> label) : base(entity)
    {
        Parent = parent;
        ColumnIndex = columnIndex * 2;
        CanBeNull = false;
        IsReadOnly = false;
        ColumnName = columnName;
        OriginalValueTooltip = null;
        ActionCommand = action;
        AutoDispose(label.SubscribeAction(s =>
        {
            ActionLabel = s;
            RaisePropertyChanged(nameof(ActionLabel));
        }));
    }

    public void UpdateFromString(string newValue)
    {
        if (ParameterValue == null)
            return;

        ParameterValue.UpdateFromString(newValue);
    }

    public string? StringValue => ParameterValue?.ValueAsString;

    public override string? ToString()
    {
        return ParameterValue?.String;
    }
}