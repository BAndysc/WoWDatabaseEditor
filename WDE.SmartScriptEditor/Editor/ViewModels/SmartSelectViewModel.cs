using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Conditions.Data;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartSelectViewModel : ObservableBase, IDialog
    {
        private readonly SourceList<SmartItem> items = new();

        public SmartSelectViewModel(
            string title,
            SmartType type,
            Func<SmartGenericJsonData, bool> predicate,
            ISmartDataManager smartDataManager,
            IConditionDataManager conditionDataManager)
        {
            Title = title;
            MakeItems(type, predicate, smartDataManager, conditionDataManager);
            
            ReadOnlyObservableCollection<SmartItemsGroup> l;
            var currentFilter = this.WhenValueChanged(t => t.SearchBox)
                .Select<string, Func<SmartItem, bool>>(text =>
                {
                    if (string.IsNullOrEmpty(text))
                        return _ => true;
                    var lower = text.ToLower();
                    return item => item.Name.ToLower().Contains(lower);
                });

            AutoDispose(items.Connect()
                .Filter(currentFilter)
                .GroupOn(t => (t.Group, t.GroupOrder))
                .Transform(group => new SmartItemsGroup(this, group))
                .DisposeMany()
                .Sort(Comparer<SmartItemsGroup>.Create((x, y) => x.GroupOrder.CompareTo(y.GroupOrder)))
                .Bind(out l)
                .Subscribe());
            FilteredItems = l;

            if (items.Count > 0)
                SelectedItem = items.Items.First();

            Cancel = new DelegateCommand(() =>
            {
                CloseCancel?.Invoke();
            });
            Accept = new DelegateCommand(() =>
            {
                if (selectedItem == null)
                    SelectedItem = FilteredItems[0][0];

                CloseOk?.Invoke();
            }, () => selectedItem != null || (FilteredItems.Count == 1 && FilteredItems[0].Count == 1));
        }

        public ReadOnlyObservableCollection<SmartItemsGroup> FilteredItems { get; }

        private string searchBox;
        public string SearchBox
        {
            get => searchBox;
            set => SetProperty(ref searchBox, value);
        }

        private SmartItem selectedItem;
        public SmartItem SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
                Accept?.RaiseCanExecuteChanged();
            }
        }
        
        private void MakeItems(SmartType type, 
            Func<SmartGenericJsonData, bool> predicate, 
            ISmartDataManager smartDataManager, 
            IConditionDataManager conditionDataManager)
        {
            int order = 0;
            int groupOrder = 0;
            foreach (var smartDataGroup in smartDataManager.GetGroupsData(type))
            {
                groupOrder++;
                foreach (var member in smartDataGroup.Members)
                {
                    if (smartDataManager.Contains(type, member))
                    {
                        SmartGenericJsonData data = smartDataManager.GetDataByName(type, member);
                        if (predicate != null && !predicate(data))
                            continue;
                     
                        SmartItem i = new();   
                        i.Group = smartDataGroup.Name;
                        i.Name = data.NameReadable;
                        i.Id = data.Id;
                        i.Help = data.Help;
                        i.IsTimed = data.IsTimed;
                        i.Deprecated = data.Deprecated;
                        i.Data = data;
                        i.Order = order++;
                        i.GroupOrder = groupOrder;

                        items.Add(i);
                    }
                }
            }

            if (type == SmartType.SmartCondition)
            {
                foreach (var conditionDataGroup in conditionDataManager.GetConditionGroups())
                {
                    groupOrder++;
                    foreach (var member in conditionDataGroup.Members)
                    {
                        if (conditionDataManager.HasConditionData(member))
                        {
                            ConditionJsonData data = conditionDataManager.GetConditionData(member);

                            SmartItem i = new();
                            i.Group = conditionDataGroup.Name;
                            i.Name = data.NameReadable;
                            i.Id = data.Id;
                            i.Help = data.Help;
                            i.Deprecated = false;
                            i.ConditionData = data;
                            i.GroupOrder = groupOrder;
                            i.Order = order++;

                            items.Add(i);
                        }
                    }
                }   
            }
        }

        public DelegateCommand Cancel { get; }
        public DelegateCommand Accept { get; }
        public int DesiredWidth => 750;
        public int DesiredHeight => 650;
        public string Title { get; }
        public bool Resizeable => true;
        public event Action CloseCancel;
        public event Action CloseOk;
    }
    
    public class SmartItem
    {
        public SmartGenericJsonData Data;
        public ConditionJsonData ConditionData;
        public string Name { get; set; }
        public bool Deprecated { get; set; }
        public string Help { get; set; }
        public int Id { get; set; }
        public string Group { get; set; }
        public bool IsTimed { get; set; }
        public int Order { get; set; }
        public int GroupOrder { get; set; }
    }
    
    public class SmartItemsGroup : ObservableCollectionExtended<SmartItem>, IGrouping<(string, int), SmartItem>, IDisposable
    {
        private readonly IDisposable disposable;
        private readonly SmartSelectViewModel parent;
        
        public SmartItemsGroup(SmartSelectViewModel parent, IGroup<SmartItem, (string, int)> group) 
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            this.parent = parent;
            Key = group.GroupKey;

            parent.ToObservable(t => t.SelectedItem)
                .Subscribe(item =>
                {
                    inEvent = true;
                    if (item == null || Contains(item))
                        SelectedItem = item;
                    else
                        SelectedItem = null;
                    inEvent = false;
                });
            
            disposable = group.List
                .Connect()
                .Sort(Comparer<SmartItem>.Create((x, y) => x.Order.CompareTo(y.Order)))
                .Bind(this)
                .Subscribe();
        }

        private bool inEvent = false;
        public string Name => Key.Item1;
        public string GroupName => Key.Item1;
        public int GroupOrder => Key.Item2;
        public (string, int) Key { get; private set; }
        
        private SmartItem selectedItem;
        public SmartItem SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!inEvent)
                    parent.SelectedItem = value;
                else
                {
                    selectedItem = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }
        }

        public void Dispose() => disposable.Dispose();
    }
}