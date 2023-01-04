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
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Services;

namespace WDE.EventAiEditor.Editor.ViewModels
{
    public class EventAiSelectViewModel : ObservableBase, IDialog
    {
        private readonly IFavouriteEventAiService favourites;
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

            if (cancellationToken.IsCancellationRequested)
            {
                currentToken = null;
                return;
            }
            
            var filtered = AllItems.OrderByDescending(f => string.IsNullOrEmpty(lower) ? -f.Order : f.Score).ToList();
            Items.OverrideWith(filtered);
            
            SelectFirstVisible();
            currentToken = null;
        }

        public EventAiSelectViewModel(
            string title,
            EventOrAction type,
            Func<EventActionGenericJsonData, bool> predicate,
            List<(uint, string)>? customItems,
            IEventAiDataManager eventAiDataManager,
            IConditionDataManager conditionDataManager,
            IFavouriteEventAiService favourites)
        {
            this.favourites = favourites;
            Title = title;
            MakeItems(type, predicate, customItems, eventAiDataManager, conditionDataManager);

            AutoDispose(this.WhenValueChanged(t => t.SearchBox)!
                .SubscribeAction(text =>
                {
                    if (currentToken != null)
                    {
                        LOG.LogInformation("Searching in progress, canceling");
                    }
                    currentToken?.Cancel();
                    var token = new CancellationTokenSource();
                    FilterAndSort(text, token, token.Token).ListenErrors();
                }));

            if (Items.Count > 0)
                SelectFirstVisible();

            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            _accept = new DelegateCommand(() =>
            {
                if (selectedItem == null)
                    SelectedItem = FindExactMatching() ?? Items.FirstOrDefault();

                CloseOk?.Invoke();
            }, () => selectedItem != null || (visibleCount == 1 || FindExactMatching() != null))
                .ObservesProperty(() => SearchBox)
                .ObservesProperty(() => SelectedItem);
            
            ToggleFavouriteCommand = new DelegateCommand<EventOrActionItem>(item =>
            {
                item.IsFavourite = !item.IsFavourite;
            });
        }

        private EventOrActionItem? FindExactMatching()
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
        
        public List<EventOrActionItem> AllItems { get; } = new();
        public ObservableCollectionExtended<EventOrActionItem> Items { get; } = new();
        
        private string searchBox = "";
        public string SearchBox
        {
            get => searchBox;
            set => SetProperty(ref searchBox, value);
        }

        private EventOrActionItem? selectedItem;
        public EventOrActionItem? SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
                _accept?.RaiseCanExecuteChanged();
            }
        }
        
        private void MakeItems(EventOrAction type, 
            Func<EventActionGenericJsonData, bool> predicate, 
            List<(uint id, string name)>? customItems,
            IEventAiDataManager eventAiDataManager, 
            IConditionDataManager conditionDataManager)
        {
            int order = 0;
            foreach (var group in eventAiDataManager.GetGroupsData(type))
            {
                foreach (var member in group.Members)
                {
                    if (eventAiDataManager.Contains(type, member))
                    {
                        EventActionGenericJsonData data = eventAiDataManager.GetDataByName(type, member);
                        if (predicate != null && !predicate(data))
                            continue;

                        EventOrActionItem i = new(group.Name, favourites.IsFavourite(data.Name), (item, @is) => favourites.SetFavourite(item.EnumName, @is))
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
                    }
                }
            }

            if (customItems != null)
            {
                foreach (var customItem in customItems)
                {
                    EventOrActionItem i = new("Custom", false, null)
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
        }

        public DelegateCommand<EventOrActionItem> ToggleFavouriteCommand { get; }
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