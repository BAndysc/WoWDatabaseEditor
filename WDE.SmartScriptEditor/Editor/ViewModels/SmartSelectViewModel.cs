using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartSelectViewModel : ObservableBase, IDialog
    {
        private bool anyVisible => visibleCount > 0;
        private int visibleCount = 0;
        private CancellationTokenSource? currentToken;

        private async Task FilterAndSort(string? text, CancellationTokenSource tokenSource, CancellationToken cancellationToken)
        {
            while (currentToken != null)
            {
                await Task.Run(() => Thread.Sleep(50)).ConfigureAwait(true);
                currentToken = tokenSource;
            }
            
            var lower = text?.ToLower();
            
            // filtering on a separate thread, so that UI doesn't lag
            await Task.Run(() =>
            {
                visibleCount = 0;
                foreach (var item in Items)
                {
                    item.Score = string.IsNullOrEmpty(lower) ? 100 : (item.Name.ToLower() == lower ? 101 : FuzzySharp.Fuzz.WeightedRatio(item.SearchName, lower));
                    if (item.ShowItem)
                        visibleCount++;
                    else if (item == SelectedItem)
                        SelectedItem = null;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        currentToken = null;
                        return;
                    }
                }
            }, cancellationToken).ConfigureAwait(true);

            if (cancellationToken.IsCancellationRequested)
            {
                currentToken = null;
                return;
            }
            
            // reordering items in ListBox is very expensive, therefore we are doing a trick
            // instead of reordering them, I am only updating items on their indices
            // that works!
            var filtered = Items.OrderByDescending(f => string.IsNullOrEmpty(lower) ? -f.Order : f.Score).Select(f => new SmartItem().Update(f)).ToList();
            for (int i = 0; i < filtered.Count; ++i)
                Items[i].Update(filtered[i]);
            
            SelectedItem ??= Items.FirstOrDefault(f => f.ShowItem);

            currentToken = null;
        }

        public SmartSelectViewModel(
            string title,
            SmartType type,
            Func<SmartGenericJsonData, bool> predicate,
            List<(int, string)>? customItems,
            ISmartDataManager smartDataManager,
            IConditionDataManager conditionDataManager)
        {
            Title = title;
            MakeItems(type, predicate, customItems, smartDataManager, conditionDataManager);

            AutoDispose(this.WhenValueChanged(t => t.SearchBox)!
                .SubscribeAction(text =>
                {
                    if (currentToken != null)
                    {
                        Console.WriteLine("Searching in progress, canceling");
                    }
                    currentToken?.Cancel();
                    var token = new CancellationTokenSource();
                    FilterAndSort(text, token, token.Token).ListenErrors();
                }));

            if (Items.Count > 0)
                SelectedItem = Items[0];

            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            Accept = new DelegateCommand(() =>
            {
                if (selectedItem == null)
                    SelectedItem = FindExactMatching() ?? Items.FirstOrDefault(f => f.ShowItem);

                CloseOk?.Invoke();
            }, () => selectedItem != null || (visibleCount == 1 || FindExactMatching() != null))
                .ObservesProperty(() => SearchBox)
                .ObservesProperty(() => SelectedItem);
        }

        private SmartItem? FindExactMatching()
        {
            if (string.IsNullOrEmpty(SearchBox.Trim()))
                return null;
            
            var searchLowerCase = SearchBox.Trim().ToLower();
            
            foreach (var item in Items)
            {
                if (item.Name.ToLower() == searchLowerCase)
                    return item;
            }

            return null;
        }
        
        public void SelectFirstVisible()
        {
            if (visibleCount > 0)
                SelectedItem = Items.FirstOrDefault(f => f.ShowItem);
        }
        
        public ObservableCollectionExtended<SmartItem> Items { get; } = new();
        
        private string searchBox = "";
        public string SearchBox
        {
            get => searchBox;
            set => SetProperty(ref searchBox, value);
        }

        private SmartItem? selectedItem;
        public SmartItem? SelectedItem
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
            List<(int id, string name)>? customItems,
            ISmartDataManager smartDataManager, 
            IConditionDataManager conditionDataManager)
        {
            int order = 0;
            foreach (var smartDataGroup in smartDataManager.GetGroupsData(type))
            {
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
                        i.SearchName = data.SearchTags == null ? data.NameReadable : $"{data.NameReadable} {data.SearchTags}";
                        i.Id = data.Id;
                        i.Help = data.Help;
                        i.IsTimed = data.IsTimed;
                        i.Deprecated = data.Deprecated;
                        i.Order = order++;
                        i.EnumName = data.Name;

                        Items.Add(i);
                    }
                }
            }

            if (customItems != null)
            {
                foreach (var customItem in customItems)
                {
                    SmartItem i = new();   
                    i.Group = "Custom";
                    i.Name = customItem.name;
                    i.SearchName = customItem.name;
                    i.CustomId = customItem.id;
                    i.IsTimed = false;
                    i.Deprecated = false;
                    i.Order = order++;

                    Items.Add(i);
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
                            i.SearchName = data.NameReadable;
                            i.Name = data.NameReadable;
                            i.Id = data.Id;
                            i.Help = data.Help;
                            i.Deprecated = false;
                            i.Order = order++;
                            i.EnumName = data.Name;

                            Items.Add(i);
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
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}