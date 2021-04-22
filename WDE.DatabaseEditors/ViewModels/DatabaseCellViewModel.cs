using System;
using System.Reactive.Disposables;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels
{
    public class DatabaseCellViewModel : ObservableBase
    {
        public DatabaseRowViewModel Parent { get; }
        public IDatabaseField TableField { get; }
        public IParameterValue ParameterValue { get; }
        public bool IsVisible { get; private set; } = true;
        public bool IsModified { get; private set; }
        public string? OriginalValueTooltip { get; private set; }
        public bool CanBeNull => Parent.CanBeNull;
        public bool CanBeSetToNull => CanBeNull && !Parent.IsReadOnly;
        public bool CanBeReverted => !Parent.IsReadOnly;

        public DatabaseCellViewModel(DatabaseRowViewModel parent, IDatabaseField tableField, IParameterValue parameterValue, IObservable<bool>? cellIsVisible)
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
                OriginalValueTooltip =
                    tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
                RaisePropertyChanged(nameof(OriginalValueTooltip));
                RaisePropertyChanged(nameof(AsBoolValue));
            }));
        }

        public bool AsBoolValue
        {
            get => ((ParameterValue as ParameterValue<long>)?.Value ?? 0) != 0;
            set
            {
                if (ParameterValue is ParameterValue<long> longParam)
                {
                    longParam.Value = value ? 1 : 0;
                }
            }
        }
    }
}