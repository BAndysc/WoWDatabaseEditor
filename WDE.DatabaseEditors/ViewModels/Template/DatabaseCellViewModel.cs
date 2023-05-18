using System;
using System.Windows.Input;
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
        public bool IsModified => TableField?.IsModified ?? false;
        public string? OriginalValueTooltip { get; private set; }
        public bool CanBeNull => Parent.CanBeNull;
        public bool CanBeSetToNull => CanBeNull && !Parent.IsReadOnly;
        public bool CanBeReverted => !Parent.IsReadOnly;
        public string? ActionLabel { get; private set; }
        public ICommand? ActionCommand { get; }
        private bool inConstructor = true;

        public DatabaseCellViewModel(DatabaseRowViewModel parent, 
            DatabaseEntity parentEntity, 
            IDatabaseField tableField, 
            IParameterValue parameterValue, 
            IObservable<bool>? cellIsVisible) : base(parentEntity)
        {
            Watch(tableField, tf => tf.IsModified, nameof(IsModified));
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

        public DatabaseCellViewModel(DatabaseRowViewModel parent, 
            DatabaseEntity parentEntity,
            ICommand command,
            string actionLabel) : base(parentEntity)
        {
            Parent = parent;
            ActionCommand = command;
            ActionLabel = actionLabel;
            inConstructor = false;
        }
        
        public DatabaseCellViewModel(DatabaseRowViewModel parent, 
            DatabaseEntity parentEntity,
            ICommand command,
            System.IObservable<string> actionLabel) : base(parentEntity)
        {
            Parent = parent;
            ActionCommand = command;
            AutoDispose(actionLabel.SubscribeAction(str =>
            {
                ActionLabel = str;
                RaisePropertyChanged(nameof(ActionLabel));
            }));
            inConstructor = false;
        }
        
        public void UpdateFromString(string newValue)
        {
            if (ParameterValue == null)
                return;

            ParameterValue.UpdateFromString(newValue);
        }
    }
}