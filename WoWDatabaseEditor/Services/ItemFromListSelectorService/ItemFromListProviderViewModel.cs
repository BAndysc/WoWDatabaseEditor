using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.ItemFromListSelectorService
{ 
    public abstract class ItemFromListProviderViewModel<T> : ObservableBase, IDialog where T : notnull
    {
        public Dictionary<T, SelectOption>? Items { get; }
        protected ReactiveProperty<Func<CheckableSelectOption<T>, bool>> currentFilter;
        protected SourceList<CheckableSelectOption<T>> items;
        private CheckableSelectOption<T>? pseudoItem;
        protected readonly bool asFlags;
        protected string search = "";
        
        public ItemFromListProviderViewModel(Dictionary<T, SelectOption>? items, 
            IComparer<CheckableSelectOption<T>> comparer, 
            Func<T, bool> shouldBeSelected,
            bool asFlags, 
            T? current = default, 
            string? title = null)
        {
            Items = items;
            Title = title ?? "Picker";
            this.asFlags = asFlags;
            
            this.items = AutoDispose(new SourceList<CheckableSelectOption<T>>());
            ReadOnlyObservableCollection<CheckableSelectOption<T>> outFilteredList;
            currentFilter = AutoDispose(new ReactiveProperty<Func<CheckableSelectOption<T>, bool>>(_ => true,
                Compare.CreateEqualityComparer<Func<CheckableSelectOption<T>, bool>>((_, _) => false, _ => 0)));
            AutoDispose(this.items.Connect()
                .Filter(currentFilter)
                .Sort(comparer)
                .Bind(out outFilteredList)
                .Subscribe());
            FilteredItems = outFilteredList;

            CheckableSelectOption<T>? selected = null;
            if (items != null)
            {
                this.items.Edit(list =>
                {
                    foreach (T key in items.Keys)
                    {
                        bool isSelected = shouldBeSelected(key);
                        var item = new CheckableSelectOption<T>(key, items[key], isSelected);
                        if (isSelected)
                            selected = item;
                        list.Add(item);
                    }
                });
            }

            SelectedItem = selected;

            Columns = new ObservableCollection<ColumnDescriptor>
            {
                new("Key", "Entry", 80),
                new("Name", "Name", 220),
                new("Description", "Description", 320)
            };

            if (asFlags)
                Columns.Insert(0, new ColumnDescriptor("", "IsChecked", 35, true));

            if (items == null || items.Count == 0)
                SearchText = current != null ? current.ToString() ?? "" : "";

            ShowItemsList = items?.Count > 0;
            DesiredHeight = ShowItemsList ? 670 : 130;
            DesiredWidth = ShowItemsList ? 800 : 400;
            accept = new DelegateCommand(() =>
            {
                if (SelectedItem == null && FilteredItems.Count == 1)
                    SelectedItem = FilteredItems[0];
                CloseOk?.Invoke();
            }, () => asFlags || SelectedItem != null || FilteredItems.Count == 1 || int.TryParse(SearchText, out _));
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            
            FilteredItems.ObserveCollectionChanges().Subscribe(_ => accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SearchText).Subscribe(_ => accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SelectedItem).Subscribe(_ => accept.RaiseCanExecuteChanged());
        }

        public ObservableCollection<ColumnDescriptor> Columns { get; set; }
        public ReadOnlyObservableCollection<CheckableSelectOption<T>> FilteredItems { get; }
        
        private CheckableSelectOption<T>? selectedItem;
        public CheckableSelectOption<T>? SelectedItem
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

                if (!asFlags)
                {
                    if (pseudoItem != null)
                        items.Remove(pseudoItem);
                    if (Items != null && StringToT(value, out var searchedKey) && !Items.ContainsKey(searchedKey))
                    {
                        pseudoItem = new CheckableSelectOption<T>(searchedKey, new SelectOption("Pick non existing"),
                            false);
                        items.Add(pseudoItem);
                    }   
                }

                var lowerSearchText = SearchText.ToLower();
                if (string.IsNullOrEmpty(SearchText))
                    currentFilter.Value = _ => true;
                else if (typeof(T) == typeof(long) && long.TryParse(SearchText, out var searchNumber))
                    currentFilter.Value = model =>
                    {
                        if (model!.Entry!.ToString()!.Contains(SearchText))
                            return true;
                        return model.Name.ToLower().Contains(lowerSearchText);
                    };
                else
                   currentFilter.Value = model => model.Name.ToLower().Contains(lowerSearchText);
            }
        }

        public abstract T GetEntry();

        protected abstract bool StringToT(string str, out T result);

        public bool ShowItemsList { get; }
        private DelegateCommand accept;
        public ICommand Accept => accept;
        public ICommand Cancel { get; }
        public int DesiredWidth { get; }
        public int DesiredHeight { get; }
        public string Title { get; }
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class LongItemFromListProviderViewModel : ItemFromListProviderViewModel<long>
    {
        public override long GetEntry()
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

        protected override bool StringToT(string str, out long result)
        {
            return long.TryParse(str, out result);
        }

        public LongItemFromListProviderViewModel(Dictionary<long, SelectOption>? items, bool asFlags, long? current = default, string? title = null) 
            : base(items, 
                Comparer<CheckableSelectOption<long>>.Create((x, y) => x.Entry.CompareTo(y.Entry)), 
                key => (current != null) && ((current == 0 && key == 0) || (key > 0) && (current & key) == key), 
                asFlags, current ?? 0, title)
        {
        }
    }
    
    public class StringItemFromListProviderViewModel : ItemFromListProviderViewModel<string>
    {
        public override string GetEntry()
        {
            if (asFlags)
                return string.Join(" ", items.Items.Where(i => i.IsChecked).Select(i => i.Entry));

            if (SelectedItem != null)
                return SelectedItem.Entry;

            return SearchText;
        }

        protected override bool StringToT(string str, out string result)
        {
            result = str;
            return true;
        }

        public StringItemFromListProviderViewModel(Dictionary<string, SelectOption>? items, bool multiSelect,
            string? current = default, string? title = null)
            : base(items,
                Comparer<CheckableSelectOption<string>>.Create((x, y) =>
                {
                    if (long.TryParse(x.Entry, out var e1) && long.TryParse(y.Entry, out var e2))
                        return e1.CompareTo(e2);
                    return x.Entry.CompareTo(y.Entry);
                }),
                GenerateSelector(multiSelect, current), 
                multiSelect, current, title)
        {
        }

        private static Func<string, bool> GenerateSelector(bool multiSelect, string? current)
        {
            if (multiSelect == false)
                return key => key == current;
            if (current == null)
                return key => false;

            var spells = current.Split(' ').Where(i => !string.IsNullOrEmpty(i)).ToHashSet();
            return key => spells.Contains(key);
        }
    }

    public class FloatItemFromListProviderViewModel : ItemFromListProviderViewModel<float>
    {
        public FloatItemFromListProviderViewModel(Dictionary<float, SelectOption>? items, float current = default, string? title = null) : 
            base(items, Comparer<CheckableSelectOption<float>>.Create((x, y) => x.Entry.CompareTo(y.Entry))
                , _ => false, false, current, title)
        {
        }

        public override float GetEntry()
        {
            if (SelectedItem != null)
                return SelectedItem.Entry;
            if (float.TryParse(SearchText, out var ret))
                return ret;
            return 0;
        }

        protected override bool StringToT(string str, out float result)
        {
            return float.TryParse(str, out result);
        }
    }
    
    public class CheckableSelectOption<T> : INotifyPropertyChanged
    {
        public CheckableSelectOption(T entry, SelectOption selectOption, bool isChecked)
        {
            Entry = entry;
            Name = selectOption.Name;
            Description = selectOption.Description;
            IsChecked = isChecked;
        }

        public bool IsChecked { get; set; }
        public T Entry { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}