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
                foreach (var item in AllItems)
                {
                    if (string.IsNullOrEmpty(lower))
                    {
                        item.Score = 100;
                    }
                    else if (item.Name.ToLower() == lower)
                    {
                        item.Score = 101;
                    } else
                    {
                        int indexOf = item.SearchName.IndexOf(lower, StringComparison.Ordinal);
                        bool contains = indexOf != -1;
                        bool isFullWorld = false;
                        if (contains)
                        {
                            isFullWorld = true;
                            if (indexOf > 0 && item.SearchName[indexOf - 1] != ' ')
                                isFullWorld = false;
                            indexOf += lower.Length;
                            if (indexOf < item.SearchName.Length && item.SearchName[indexOf] != ' ')
                                isFullWorld = false;
                        }
                        var score = FuzzySharp.Fuzz.WeightedRatio(item.SearchName, lower);
                        item.Score = contains ? (Math.Max(score, isFullWorld ? 85 : 62)) : score;
                    }
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
            
            var filtered = AllItems.OrderByDescending(f => string.IsNullOrEmpty(lower) ? -f.Order : f.Score).ToList();
            Items.OverrideWith(filtered);
            
            SelectedItem ??= Items.FirstOrDefault();
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
                    SelectedItem = FindExactMatching() ?? Items.FirstOrDefault();

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
            SelectedItem = Items.FirstOrDefault();
        }
        
        public List<SmartItem> AllItems { get; } = new();
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

                        SmartItem i = new()
                        {
                            Group = smartDataGroup.Name,
                            Name = data.NameReadable,
                            SearchName = data.SearchTags == null ? data.NameReadable : $"{data.NameReadable} {data.SearchTags}",
                            Id = data.Id,
                            Help = data.Help,
                            IsTimed = data.IsTimed,
                            Deprecated = data.Deprecated,
                            Order = order++,
                            EnumName = data.Name,
                        };

                        AllItems.Add(i);
                        Items.Add(i);
                    }
                }
            }

            if (customItems != null)
            {
                foreach (var customItem in customItems)
                {
                    SmartItem i = new()
                    {
                        Group = "Custom",
                        Name = customItem.name,
                        SearchName = customItem.name,
                        CustomId = customItem.id,
                        IsTimed = false,
                        Deprecated = false,
                        Order = order++
                    };

                    AllItems.Add(i);
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

                            SmartItem i = new()
                            {
                                Group = conditionDataGroup.Name,
                                SearchName = data.NameReadable,
                                Name = data.NameReadable,
                                Id = data.Id,
                                Help = data.Help ?? "",
                                Deprecated = false,
                                Order = order++,
                                EnumName = data.Name,
                            };

                            AllItems.Add(i);
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