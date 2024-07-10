using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Services;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartSelectViewModel : ObservableBase, IDialog
    {
        private readonly IFavouriteSmartsService favourites;
        private bool anyVisible => visibleCount > 0;
        private int visibleCount = 0;
        private CancellationTokenSource? currentToken;

        private async Task FilterAndSort(string? text, CancellationTokenSource tokenSource, CancellationToken cancellationToken)
        {
            while (currentToken != null)
            {
                await Task.Delay(50, cancellationToken);
                currentToken = tokenSource;
            }
            
            var lower = text?.ToLower();
            int? searchId = null;
            if (int.TryParse(text, out var textInt))
                searchId = textInt;
            
            // filtering on a separate thread, so that UI doesn't lag
            await Task.Run(() =>
            {
                visibleCount = 0;
                foreach (var item in AllItems)
                {
                    if (searchId.HasValue && searchId.Value == item.Id)
                        item.Score = 101;
                    else if (string.IsNullOrEmpty(lower))
                    {
                        item.Score = 100;
                    }
                    else if (item.Name.Equals(lower, StringComparison.InvariantCultureIgnoreCase))
                    {
                        item.Score = 101;
                    } else
                    {
                        int indexOf = item.SearchName.IndexOf(lower, StringComparison.InvariantCultureIgnoreCase);
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
            {
                using var _ = Items.SuspendNotifications();
                Items.OverrideWith(filtered);
            }
            
            SelectFirstVisible();
            currentToken = null;
        }

        public SmartSelectViewModel(
            string title,
            SmartType type,
            Func<SmartGenericJsonData, bool> predicate,
            List<(int, string)>? customItems,
            ISmartDataManager smartDataManager,
            IConditionDataManager conditionDataManager,
            IFavouriteSmartsService favourites)
        {
            this.favourites = favourites;
            Title = title;
            MakeItems(type, predicate, customItems, smartDataManager, conditionDataManager).ListenErrors();

            AutoDispose(this.WhenValueChanged(t => t.SearchBox)!
                .SubscribeAction(DoFilterNow));

            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            _accept = new DelegateCommand(() =>
            {
                if (selectedItem == null)
                    SelectedItem = FindExactMatching() ?? Items.FirstOrDefault();

                CloseOk?.Invoke();
            }, () => selectedItem != null || (visibleCount == 1 || FindExactMatching() != null))
                .ObservesProperty(() => SearchBox)
                .ObservesProperty(() => SelectedItem);
            
            ToggleFavouriteCommand = new DelegateCommand<SmartItem>(item =>
            {
                item.IsFavourite = !item.IsFavourite;
            });
        }

        private void DoFilterNow(string? text)
        {
            if (currentToken != null)
            {
                LOG.LogWarning("Searching in progress, canceling");
            }

            currentToken?.Cancel();
            var token = new CancellationTokenSource();
            FilterAndSort(text, token, token.Token).ListenErrors();
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
            SelectedItem = Items.FirstOrDefault(i => i.IsFavourite && i.ShowItem) ?? Items.FirstOrDefault(i => i.ShowItem);
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
                _accept?.RaiseCanExecuteChanged();
            }
        }
        
        private async Task MakeItems(SmartType type, 
            Func<SmartGenericJsonData, bool> predicate, 
            List<(int id, string name)>? customItems,
            ISmartDataManager smartDataManager, 
            IConditionDataManager conditionDataManager)
        {
            await Task.Delay(1); // add small delay for UI to render
            int order = 0;
            foreach (var smartDataGroup in await smartDataManager.GetGroupsData(type))
            {
                foreach (var member in smartDataGroup.Members)
                {
                    if (smartDataManager.Contains(type, member))
                    {
                        SmartGenericJsonData data = smartDataManager.GetDataByName(type, member);
                        
                        if (data.Deprecated)
                            continue;

                        if (predicate != null && !predicate(data))
                            continue;

                        SmartItem i = new(smartDataGroup.Name, favourites.IsFavourite(data.Name), (item, @is) => favourites.SetFavourite(item.EnumName, @is))
                        {
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
                        //if (order % 50 == 0)
                        //    await Task.Delay(1); // add small delay for UI to render
                    }
                }
            }

            if (customItems != null)
            {
                foreach (var customItem in customItems)
                {
                    SmartItem i = new("Custom", false, null)
                    {
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

                            SmartItem i = new(conditionDataGroup.Name, favourites.IsFavourite(data.Name), (item, @is) => favourites.SetFavourite(item.EnumName, @is))
                            {
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
            
            if (Items.Count > 0 && SelectedItem == null)
                SelectFirstVisible();
            
            if (!string.IsNullOrEmpty(searchBox))
                DoFilterNow(searchBox);
        }

        public DelegateCommand<SmartItem> ToggleFavouriteCommand { get; }
        public ICommand Cancel { get; }
        private DelegateCommand _accept { get; }
        public ICommand Accept => _accept;
        public int DesiredWidth => 750;
        public int DesiredHeight => 650;
        public string Title { get; }
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}