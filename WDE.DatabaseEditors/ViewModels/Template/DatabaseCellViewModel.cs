using System;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels.Template
{
    public class DatabaseCellViewModel : BaseDatabaseCellViewModel
    {
        public DatabaseRowViewModel Parent { get; }
        public IDatabaseField? TableField { get; }
        public bool IsVisible { get; private set; } = true;
        public bool IsModified { get; private set; }
        public string? OriginalValueTooltip { get; private set; }
        public bool CanBeNull => Parent.CanBeNull;
        public bool CanBeSetToNull => CanBeNull && !Parent.IsReadOnly;
        public bool CanBeReverted => !Parent.IsReadOnly;
        private bool inConstructor = true;

        public DatabaseCellViewModel(DatabaseRowViewModel parent, 
            DatabaseEntity parentEntity, 
            IDatabaseField tableField, 
            IParameterValue parameterValue, 
            IObservable<bool>? cellIsVisible) : base(parentEntity)
        {
            Link(tableField, tf => tf.IsModified, () => IsModified);
            Parent = parent;
            TableField = tableField;
            ParameterValue = parameterValue;

            if (cellIsVisible != null)
                AutoDispose(cellIsVisible.Subscribe(v =>
                {
                    IsVisible = v;
                    RaisePropertyChanged(nameof(IsVisible));
                }));

            AutoDispose(parameterValue.ToObservable().SubscribeAction(_ =>
            {
                if (!inConstructor)
                  parent.AnyFieldModified();
                OriginalValueTooltip =
                    tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
                RaisePropertyChanged(nameof(OriginalValueTooltip));
                RaisePropertyChanged(nameof(AsBoolValue));
            }));
            
            if (UseItemPicker)
            {
                AutoDispose(ParameterValue.ToObservable().Subscribe(_ => RaisePropertyChanged(nameof(OptionValue))));
            }

            if (UseFlagsPicker)
            {
                AutoDispose(ParameterValue.ToObservable().Subscribe(_ => RaisePropertyChanged(nameof(AsLongValue))));
            }
            inConstructor = false;
        }

        public DatabaseCellViewModel(DatabaseRowViewModel parent, 
            DatabaseEntity parentEntity,
            IParameterValue parameterValue) : base(parentEntity)
        {
            Parent = parent;
            ParameterValue = parameterValue;

            AutoDispose(parameterValue.ToObservable().SubscribeAction(_ =>
            {
                if (!inConstructor)
                    parent.AnyFieldModified();
            }));
            inConstructor = false;
        }
    }
}