using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Extensions;

namespace WoWDatabaseEditorCore.Services.ItemFromListSelectorService
{
    public class ItemFromListProviderViewModel : ObservableBase, IDialog
    {
        private ReactiveProperty<Func<CheckableSelectOption, bool>> currentFilter;
        private SourceList<CheckableSelectOption> items;
        private readonly bool asFlags;
        private string search = "";
        
        public ItemFromListProviderViewModel(Dictionary<long, SelectOption>? items, bool asFlags, long? current = null)
        {
            this.asFlags = asFlags;
            
            this.items = AutoDispose(new SourceList<CheckableSelectOption>());
            ReadOnlyObservableCollection<CheckableSelectOption> outFilteredList;
            currentFilter = AutoDispose(new ReactiveProperty<Func<CheckableSelectOption, bool>>(_ => true,
                Compare.Create<Func<CheckableSelectOption, bool>>((_, _) => false, _ => 0)));
            AutoDispose(this.items.Connect()
                .Filter(currentFilter)
                .Sort(Comparer<CheckableSelectOption>.Create((x, y) => x.Entry.CompareTo(y.Entry)))
                .Bind(out outFilteredList)
                .Subscribe());
            FilteredItems = outFilteredList;

            if (items != null)
            {
                this.items.Edit(list =>
                {
                    foreach (long key in items.Keys)
                    {
                        bool isSelected = current.HasValue && ((current == 0 && key == 0) || (key > 0) && (current & key) == key);
                        var item = new CheckableSelectOption(key, items[key], isSelected);
                        if (isSelected)
                            SelectedItem = item;
                        list.Add(item);
                    }
                });
            }

            Columns = new ObservableCollection<ColumnDescriptor>
            {
                new("Key", "Entry", 80),
                new("Name", "Name", 270),
                new("Description", "Description", 380)
            };

            if (asFlags)
                Columns.Insert(0, new ColumnDescriptor("", "IsChecked", 35, true));

            if (items == null || items.Count == 0)
                SearchText = current.HasValue ? current.Value.ToString() : "";

            ShowItemsList = items?.Count > 0;
            DesiredHeight = ShowItemsList ? 670 : 130;
            DesiredWidth = ShowItemsList ? 800 : 400;
            Accept = new DelegateCommand(() =>
            {
                if (SelectedItem == null && FilteredItems.Count == 1)
                    SelectedItem = FilteredItems[0];
                CloseOk?.Invoke();
            }, () => asFlags || SelectedItem != null || FilteredItems.Count == 1 || int.TryParse(SearchText, out _));
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            
            FilteredItems.ObserveCollectionChanges().Subscribe(_ => Accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SearchText).Subscribe(_ => Accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SelectedItem).Subscribe(_ => Accept.RaiseCanExecuteChanged());
        }

        public ObservableCollection<ColumnDescriptor> Columns { get; set; }
        public ReadOnlyObservableCollection<CheckableSelectOption> FilteredItems { get; }

        private CheckableSelectOption? selectedItem;
        public CheckableSelectOption? SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }
        
        public string SearchText
        {
            get => search;
            set
            {
                SetProperty(ref search, value);
                if (string.IsNullOrEmpty(SearchText))
                    currentFilter.Value = _ => true;
                else
                   currentFilter.Value = model => model.Name.ToLower().Contains(SearchText.ToLower());
            }
        }
        
        public long GetEntry()
        {
            if (asFlags)
            {
                long val = 0;
                foreach (var item in items.Items)
                {
                    if (item.IsChecked)
                        val |= item.Entry;
                }

                return val;
            }

            if (SelectedItem != null)
                return SelectedItem.Entry;

            long res;
            if (long.TryParse(SearchText, out res))
                return res;
            
            if (items.Count > 0)
            {
                return items.Items.First().Entry;
            }

            return 0;
        }

        public bool ShowItemsList { get; }
        public DelegateCommand Accept { get; }
        public ICommand Cancel { get; }
        public int DesiredWidth { get; }
        public int DesiredHeight { get; }
        public string Title => "Picker";
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class CheckableSelectOption : INotifyPropertyChanged
    {
        public CheckableSelectOption(long entry, SelectOption selectOption, bool isChecked)
        {
            Entry = entry;
            Name = selectOption.Name;
            Description = selectOption.Description;
            IsChecked = isChecked;
        }

        public bool IsChecked { get; set; }
        public long Entry { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}