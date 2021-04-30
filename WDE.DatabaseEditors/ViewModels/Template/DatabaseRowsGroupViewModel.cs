using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using DynamicData.Binding;

namespace WDE.DatabaseEditors.ViewModels.Template
{
    public class DatabaseRowsGroupViewModel : ObservableCollectionExtended<DatabaseRowViewModel>, IGrouping<(string, int), DatabaseRowViewModel>, IDisposable
    {
        private readonly IDisposable disposable;
            
        public DatabaseRowsGroupViewModel(IGroup<DatabaseRowViewModel, (string, int)> group, System.IObservable<bool> showGroup) 
        {
            Key = group.GroupKey;
            ShowGroup = showGroup;
            disposable = group.List
                .Connect()
                .Sort(Comparer<DatabaseRowViewModel>.Create((x, y) => x.Order.CompareTo(y.Order)))
                .Bind(this)
                .Subscribe();
        }

        public System.IObservable<bool> ShowGroup { get; }
        public int GroupOrder => Key.Item2;
        public string GroupName => Key.Item1;
        public (string, int) Key { get; private set; }
        public void Dispose() => disposable.Dispose();
    }
}