using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using Microsoft.Extensions.Logging;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseCellViewModel : BaseDatabaseCellViewModel, ITableCell
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
        public ColumnFullName? DbColumnName { get; }

        public DatabaseCellViewModel(int columnIndex, DatabaseColumnJson columnDefinition, DatabaseEntityViewModel parent, DatabaseEntity parentEntity, IDatabaseField? tableField, IParameterValue parameterValue) : base(parentEntity)
        {
            ColumnIndex = columnIndex * 2;
            CanBeNull = columnDefinition.CanBeNull;
            IsReadOnly = columnDefinition.IsReadOnly;
            ColumnName = columnDefinition.Name;
            DbColumnName = columnDefinition.DbColumnFullName;
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
                Parent.RaiseChanged(this, tableField?.FieldName);
                if (tableField != null)
                {
                    OriginalValueTooltip =
                        tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
                }
                RaisePropertyChanged(nameof(OriginalValueTooltip));
                RaisePropertyChanged(nameof(AsBoolValue));
            }));
            if (parameterValue.BaseParameter is ITableAffectedByParameter contextual)
            {
                if (contextual.AffectedByConditions)
                {
                    parentEntity.OnConditionsChanged += OnConditionsChanged;
                    AutoDispose(new ActionDisposable(() =>
                    {
                        parentEntity.OnConditionsChanged -= OnConditionsChanged;
                    }));
                }
                else
                {
                    var other = parent.Cells.FirstOrDefault(c => c.DbColumnName == contextual.AffectedByColumn);
                    if (other != null)
                    {
                        AutoDispose(other.ParameterValue!.ToObservable().Subscribe(_ =>
                        {
                            parameterValue.RaiseChanged();
                        }));                    
                    }
                    else
                    {
                        LOG.LogError("Couldn't find column " + contextual.AffectedByColumn);
                    }   
                }
            }
        }

        private void OnConditionsChanged(DatabaseEntity arg1, IReadOnlyList<ICondition>? arg2, IReadOnlyList<ICondition>? arg3)
        {
            ParameterValue?.RaiseChanged();
        }

        public DatabaseCellViewModel(int columnIndex, string columnName, ICommand action, DatabaseEntityViewModel parent, DatabaseEntity entity, string label) : base(entity)
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

        public DatabaseCellViewModel(int columnIndex, string columnName, ICommand action, DatabaseEntityViewModel parent, DatabaseEntity entity, System.IObservable<string> label) : 
            this(columnIndex, columnName, action, parent, entity, "")
        {
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
}