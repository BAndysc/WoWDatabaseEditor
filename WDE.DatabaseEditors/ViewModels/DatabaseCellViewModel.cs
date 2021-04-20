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
        private static ReactiveProperty<bool> alwaysTrueProperty = new ReactiveProperty<bool>(true);
        public IDatabaseField TableField { get; }
        public System.IObservable<bool> CellVisible { get; }
        public IParameterValue ParameterValue { get; }
        public bool IsVisible { get; private set; }
        public bool IsModified { get; private set; }
        public string? OriginalValueTooltip { get; private set; }

        public DatabaseCellViewModel(DatabaseRowViewModel parent, IDatabaseField tableField, IParameterValue parameterValue, System.IObservable<bool>? cellVisibleellVisible)
        {
            Link(tableField, tf => tf.IsModified, () => IsModified);
            Parent = parent;
            TableField = tableField;
            CellVisible = cellVisibleellVisible ?? new SingleObservable<bool>(this, true);
            ParameterValue = parameterValue;
            CellVisible.Subscribe(v =>
            {
                IsVisible = v;
                RaisePropertyChanged(nameof(IsVisible));
            });
            parameterValue.ToObservable().SubscribeAction(_ =>
            {
                OriginalValueTooltip = tableField.IsModified ? "Original value: " + parameterValue.OriginalString : null;
                RaisePropertyChanged(nameof(OriginalValueTooltip));
                RaisePropertyChanged(nameof(AsBoolValue));
            });
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
    
    public class SingleObservable<T> : System.IObservable<T>
    {
        private readonly DatabaseCellViewModel databaseCellViewModel;
        private readonly T value;

        public SingleObservable(DatabaseCellViewModel databaseCellViewModel, T value)
        {
            this.databaseCellViewModel = databaseCellViewModel;
            this.value = value;
        }
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(value);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}