using System;
using System.Windows.Input;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseCellViewModel : BaseDatabaseCellViewModel
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

        public DatabaseCellViewModel(int columnIndex, DatabaseColumnJson columnDefinition, DatabaseEntityViewModel parent, DatabaseEntity parentEntity, IDatabaseField tableField, IParameterValue parameterValue) : base(parentEntity)
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

            AutoDispose(parameterValue.ToObservable().SubscribeAction(_ =>
            {
                OriginalValueTooltip =
                    tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
                RaisePropertyChanged(nameof(OriginalValueTooltip));
                RaisePropertyChanged(nameof(AsBoolValue));
            }));
        }

        public DatabaseCellViewModel(int columnIndex, string columnName, ICommand action, DatabaseEntityViewModel parent, DatabaseEntity entity, System.IObservable<string> label) : base(entity)
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
    }
}