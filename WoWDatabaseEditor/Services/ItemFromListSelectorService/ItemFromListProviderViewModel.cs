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
        public AsyncFilteredObservableList<CheckableSelectOption<T>> FilteredItems { get; }
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
            
            CheckableSelectOption<T>? selected = null;
            List<CheckableSelectOption<T>> list = new();
            if (items != null)
            {
                foreach (T key in items.Keys)
                {
                    bool isSelected = shouldBeSelected(key);
                    var item = new CheckableSelectOption<T>(key, items[key], isSelected);
                    if (isSelected)
                        selected = item;
                    list.Add(item);
                }
            }

            FilteredItems = new AsyncFilteredObservableList<CheckableSelectOption<T>>(list,
                (search, token, source) =>
                {
                    var result = new List<CheckableSelectOption<T>>();
                    
                    long searchNumber;
                    var lowerSearchText = search.Trim().ToLower();
                    if (typeof(T) == typeof(long) && long.TryParse(search, out searchNumber))
                    {
                        foreach (var element in source)
                        {
                            // todo: get rid of boxing
                            var entryAsLong = (long)(object)element.Entry;
                            if (entryAsLong.Contains(search))
                                result.Add(element);
                            else if (asFlags && (searchNumber & entryAsLong) == entryAsLong)
                                result.Add(element);
                            else if (element.Name.Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase))
                                result.Add(element);
                            if (token.IsCancellationRequested)
                                return null;
                        }
                    }
                    else
                    {
                        foreach (var element in source)
                        {
                            if (element.Name.Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase))
                                result.Add(element);
                            if (token.IsCancellationRequested)
                                return null;
                        }
                    }

                    return result;
                }, comparer);

            SelectedItem = selected;

            Columns = new ObservableCollection<ColumnDescriptor>
            {
                ColumnDescriptor.TextColumn("Key", "Entry", 80),
                ColumnDescriptor.TextColumn("Name", "Name", 220),
                ColumnDescriptor.TextColumn("Description", "Description", 320)
            };

            if (asFlags)
                Columns.Insert(0, ColumnDescriptor.CheckBoxColumn("", "IsChecked", 35));

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
            }, () => asFlags || SelectedItem != null || FilteredItems.Count == 1 || CanChooseText(SearchText));
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            
            FilteredItems.ObserveCollectionChanges().Subscribe(_ => accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SearchText).Subscribe(_ => accept.RaiseCanExecuteChanged());
            this.WhenPropertyChanged(t => t.SelectedItem).Subscribe(_ => accept.RaiseCanExecuteChanged());
        }

        protected abstract bool CanChooseText(string searchText);

        public ObservableCollection<ColumnDescriptor> Columns { get; set; }
        
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
                        FilteredItems.RemoveAddedElement(pseudoItem);
                    if (Items != null && StringToT(value, out var searchedKey) && !Items.ContainsKey(searchedKey))
                    {
                        pseudoItem = new CheckableSelectOption<T>(searchedKey, new SelectOption("Pick non existing"),
                            false);
                        FilteredItems.Add(pseudoItem);
                    }   
                }

                FilteredItems.SetFilter(value);
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
        protected override bool CanChooseText(string searchText) => long.TryParse(SearchText, out _);

        public override long GetEntry()
        {
            if (asFlags)
            {
                long val = 0;
                foreach (var item in FilteredItems.SourceItems)
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
            
            if (FilteredItems.Count > 0)
            {
                return FilteredItems[0].Entry;
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
        protected override bool CanChooseText(string searchText) => true;

        public override string GetEntry()
        {
            if (asFlags)
                return string.Join(" ", FilteredItems.SourceItems.Where(i => i.IsChecked).Select(i => i.Entry));

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

        protected override bool CanChooseText(string searchText) => float.TryParse(SearchText, out _);

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
