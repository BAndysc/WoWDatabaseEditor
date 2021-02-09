using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Extensions;
using WoWDatabaseEditorCore.Services.ItemFromListSelectorService;

namespace WoWDatabaseEditorCore.Services.CreatureEntrySelectorService
{
    public class GenericSelectorDialogViewModel<T> : BindableBase, IDialog
    {
        private readonly Func<T, uint> entryGetter;
        private readonly Func<T, string> index;
        private ReactiveProperty<Func<T, bool>> currentFilter;
        private SourceList<T> items;
        private string search = "";

        public GenericSelectorDialogViewModel(IEnumerable<ColumnDescriptor> columns,
            IEnumerable<T> collection,
            Func<T, uint> entryGetter,
            Func<T, string> index)
        {
            this.entryGetter = entryGetter;
            this.index = index;
            items = new SourceList<T>();
            ReadOnlyObservableCollection<T> l;
            currentFilter = new ReactiveProperty<Func<T, bool>>(_ => true, Compare.Create<Func<T, bool>>((_, _) => false, _ => 0));
            items
                .Connect()
                .Filter(currentFilter)
                .Sort(Comparer<T>.Create((x, y) => entryGetter(x).CompareTo(entryGetter(y))))
                .Bind(out l)
                .Subscribe();
            FilteredItems = l;

            Columns = new ObservableCollection<ColumnDescriptor>();

            foreach (var column in columns)
                Columns.Add(column);

            items.AddRange(collection);

            Accept = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public ObservableCollection<ColumnDescriptor> Columns { get; set; }
        public ReadOnlyObservableCollection<T> FilteredItems { get; }

        public T? SelectedItem { get; set; }

        public string SearchText
        {
            get => search;
            set
            {
                SetProperty(ref search, value);
                
                if (string.IsNullOrEmpty(SearchText))
                    currentFilter.Value = _ => true;
                else
                    currentFilter.Value = model => index(model).ToLower().Contains(SearchText.ToLower());
            }
        }


        public uint GetEntry()
        {
            if (SelectedItem != null)
                return entryGetter(SelectedItem);

            uint res;
            uint.TryParse(SearchText, out res);
            return res;
        }

        public ICommand Accept { get; }
        
        public int DesiredWidth => 380;
        public int DesiredHeight => 500;
        public string Title => "Picker";
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}