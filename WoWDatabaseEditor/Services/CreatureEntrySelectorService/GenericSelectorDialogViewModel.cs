using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.CreatureEntrySelectorService
{
    public class GenericSelectorDialogViewModel<T> : BindableBase, IDialog
    {
        private readonly Func<T, int> entryGetter;
        private readonly Func<T, string> index;
        private SourceList<T> items;
        private string search = "";

        public GenericSelectorDialogViewModel(IEnumerable<ColumnDescriptor> columns,
            IEnumerable<T> collection,
            Func<T, int> entryGetter,
            Func<T, string> index)
        {
            this.entryGetter = entryGetter;
            this.index = index;
            items = new SourceList<T>();
            ReadOnlyObservableCollection<T> l;
            var currentFilter = this.WhenValueChanged(t => t.SearchText).Select<string?, Func<T, bool>>(val =>
            {
                if (string.IsNullOrEmpty(val))
                    return model => true;
                var lowerCase = val.ToLower();
                return model => index(model).ToLower().Contains(lowerCase);
            });
            items
                .Connect()
                .Filter(currentFilter, ListFilterPolicy.ClearAndReplace)
                .Sort(Comparer<T>.Create((x, y) => entryGetter(x).CompareTo(entryGetter(y))))
                .Bind(out l)
                .Subscribe();
            FilteredItems = l;

            Columns = new ObservableCollection<ColumnDescriptor>();

            foreach (var column in columns)
                Columns.Add(column);

            items.AddRange(collection);

            accept = new DelegateCommand(() =>
            {
                if (SelectedItem == null && FilteredItems.Count == 1)
                    SelectedItem = FilteredItems[0];
                CloseOk?.Invoke();
            }, () => SelectedItem != null || FilteredItems.Count == 1 || int.TryParse(SearchText, out _));
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

            FilteredItems.ObserveCollectionChanges().Subscribe(_ => accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SearchText).Subscribe(_ => accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SelectedItem).Subscribe(_ => accept.RaiseCanExecuteChanged());
        }

        public ObservableCollection<ColumnDescriptor> Columns { get; set; }
        public ReadOnlyObservableCollection<T> FilteredItems { get; }

        private T? selectedItem;
        public T? SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        public string SearchText
        {
            get => search;
            set => SetProperty(ref search, value);
        }
        
        public int GetEntry()
        {
            if (SelectedItem != null)
                return entryGetter(SelectedItem);

            int res;
            int.TryParse(SearchText, out res);
            return res;
        }

        private DelegateCommand accept;
        public ICommand Accept => accept;
        public ICommand Cancel { get; }
        public int DesiredWidth => 380;
        public int DesiredHeight => 500;
        public string Title => "Picker";
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}