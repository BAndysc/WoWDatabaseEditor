using System;
using System.Reactive.Disposables;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels
{
    public class DatabaseCellViewModel : BindableBase
    {
        public DatabaseEntityViewModel ParentEntity { get; }
        private static ReactiveProperty<bool> alwaysTrueProperty = new ReactiveProperty<bool>(true);
        public string CategoryName { get; }
        public string FieldName { get; }
        public bool IsReadOnly { get; }
        public IDatabaseField TableField { get; }
        public int CategoryIndex { get; }
        public int Order { get; }
        public System.IObservable<bool> CellVisible { get; }
        public IParameterValue ParameterValue { get; }
        public bool IsVisible { get; private set; }
        

        public DatabaseCellViewModel(DatabaseEntityViewModel parent, IDatabaseField tableField, IParameterValue parameterValue, DbEditorTableGroupFieldJson columnData, string category, int categoryIndex, int order, System.IObservable<bool>? cellVisibleellVisible)
        {
            ParentEntity = parent;
            CategoryName = category;
            FieldName = columnData.Name;
            IsReadOnly = columnData.IsReadOnly;
            CategoryIndex = categoryIndex;
            Order = order;
            TableField = tableField;
            CellVisible = cellVisibleellVisible ?? new SingleObservable<bool>(this, true);
            ParameterValue = parameterValue;
            CellVisible.Subscribe(v =>
            {
                IsVisible = v;
                RaisePropertyChanged(nameof(IsVisible));
            });
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
            Console.WriteLine("Sub: " + databaseCellViewModel.FieldName);
            observer.OnNext(value);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}