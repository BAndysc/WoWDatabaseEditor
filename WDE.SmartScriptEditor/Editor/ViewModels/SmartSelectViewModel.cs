using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartSelectViewModel : BindableBase, IDialog
    {
        private ReactiveProperty<Func<SmartItem, bool>> currentFilter;
        private readonly SourceList<SmartItem> items = new();
        private readonly Func<SmartGenericJsonData, bool> predicate;
        private string searchBox;
        private SmartItem selectedItem;

        public SmartSelectViewModel(SmartType type,
            Func<SmartGenericJsonData, bool> predicate,
            ISmartDataManager smartDataManager,
            IConditionDataManager conditionDataManager)
        {
            this.predicate = predicate;
            MakeItems(type, smartDataManager, conditionDataManager);

            items = new SourceList<SmartItem>();
            ReadOnlyObservableCollection<SmartItem> l;
            currentFilter = new ReactiveProperty<Func<SmartItem, bool>>(_ => true, Compare.Create<Func<SmartItem, bool>>((_, _) => false, _ => 0));
            items
                .Connect()
                .Filter(currentFilter)
                .Sort(Comparer<SmartItem>.Create((x, y) => x.Name.CompareTo(y.Name)))
                .Bind(out l)
                .Subscribe();
            FilteredItems = l;

            if (items.Count > 0)
                SelectedItem = items.Items.First();

            Accept = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
            }, () => selectedItem != null);
        }

        public ReadOnlyObservableCollection<SmartItem> FilteredItems { get; }

        public string SearchBox
        {
            get => searchBox;
            set
            {
                SetProperty(ref searchBox, value);
                if (string.IsNullOrEmpty(value))
                    currentFilter.Value = _ => true;
                else
                    currentFilter.Value = item => item.Name.ToLower().Contains(SearchBox.ToLower());
            }
        }

        public SmartItem SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
                Accept?.RaiseCanExecuteChanged();
            }
        }
        
        private void MakeItems(SmartType type, ISmartDataManager smartDataManager, IConditionDataManager conditionDataManager)
        {
            foreach (var smartDataGroup in smartDataManager.GetGroupsData(type))
            {
                foreach (var member in smartDataGroup.Members)
                {
                    if (smartDataManager.Contains(type, member))
                    {
                        SmartGenericJsonData data = smartDataManager.GetDataByName(type, member);
                        if (predicate != null && predicate(data))
                            continue;
                     
                        SmartItem i = new();   
                        i.Group = smartDataGroup.Name;
                        i.Name = data.NameReadable;
                        i.Id = data.Id;
                        i.Help = data.Help;
                        i.IsTimed = data.IsTimed;
                        i.Deprecated = data.Deprecated;
                        i.Data = data;

                        items.Add(i);
                    }
                }
            }

            if (type == SmartType.SmartCondition)
            {
                foreach (var conditionDataGroup in conditionDataManager.GetConditionGroups())
                {
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

                            items.Add(i);
                        }
                    }
                }   
            }
        }

        public DelegateCommand Accept { get; }
        public int DesiredWidth => 750;
        public int DesiredHeight => 650;
        public string Title => "Pick";
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
    }
}