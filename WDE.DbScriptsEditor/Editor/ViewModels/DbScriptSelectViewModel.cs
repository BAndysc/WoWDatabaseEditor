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
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Models;
using WDE.DbScriptsEditor.Services;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // The SmartScript-style command picker dialog: search box + grouped, favouritable command
    // tiles (variants are separate tiles serializing to the same command id with presets).
    public class DbScriptSelectViewModel : ObservableBase, IDialog
    {
        // Sentinel keys for the synthetic structural entries offered when adding a new row
        // (safe: real keys are non-negative command-id | variant encodings).
        public const long WaitKey = -1;
        public const long CommentKey = -2;

        private readonly IFavouriteDbScriptCommandsService? favourites;
        private int visibleCount = 0;
        private CancellationTokenSource? currentToken;

        public DbScriptSelectViewModel(
            string title,
            long? preselectKey,
            IDbScriptDataManager dataManager,
            IFavouriteDbScriptCommandsService favourites,
            Func<DbScriptCommandDefinition, bool>? predicate = null,
            bool includeStructural = false)
        {
            this.favourites = favourites;
            Title = title;
            MakeItems(dataManager, predicate, includeStructural);

            Setup(preselectKey);
        }

        // A generic tile picker over a prebuilt item list (used for the source/target actor
        // pickers of the add-action wizard; no favourites there).
        public DbScriptSelectViewModel(string title, IEnumerable<DbScriptCommandItem> items, long? preselectKey = null)
        {
            favourites = null;
            Title = title;
            foreach (var item in items)
                AddItem(item);

            Setup(preselectKey);
        }

        private void Setup(long? preselectKey)
        {
            if (preselectKey.HasValue)
                SelectedItem = AllItems.FirstOrDefault(i => i.Key == preselectKey.Value);
            if (SelectedItem == null && Items.Count > 0)
                SelectFirstVisible();

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

            ToggleFavouriteCommand = new DelegateCommand<DbScriptCommandItem>(item =>
            {
                item.IsFavourite = !item.IsFavourite;
            });
        }

        private void MakeItems(IDbScriptDataManager dataManager,
            Func<DbScriptCommandDefinition, bool>? predicate,
            bool includeStructural)
        {
            int order = 0;

            // The action-first add flow keeps Wait/Comment reachable from its single picker
            // (the wizard offers them in its source picker instead).
            if (includeStructural)
            {
                AddItem(new DbScriptCommandItem("Timing", false, null)
                {
                    Key = WaitKey,
                    Name = "Wait",
                    SearchName = "wait delay time gap pause",
                    Help = "A time gap: every following row executes this much later (squashed into the delay column on save).",
                    IsWait = true,
                    Order = order++,
                });
                AddItem(new DbScriptCommandItem("Timing", false, null)
                {
                    Key = CommentKey,
                    Name = "Comment",
                    SearchName = "comment note text",
                    Help = "A standalone comment row (saved into the comments column of the next action).",
                    Order = order++,
                });
            }

            // commands_groups.json defines the group presentation order; commands keep their
            // commands.json order within a group. Unknown groups go last, in encounter order.
            var groupOrder = new Dictionary<string, int>();
            foreach (var g in dataManager.Groups)
                groupOrder[g.Name] = groupOrder.Count;
            int UnknownGroup(string? name) => groupOrder.TryGetValue(name ?? "", out var i) ? i : int.MaxValue;

            foreach (var def in dataManager.AllCommands.OrderBy(d => UnknownGroup(d.Group)))
            {
                if (def.Deprecated)
                    continue;
                if (predicate != null && !predicate(def))
                    continue;

                var group = string.IsNullOrEmpty(def.Group) ? "Other" : def.Group;
                AddItem(new DbScriptCommandItem(group, favourites!.IsFavourite(def.Name), SetFavourite)
                {
                    Key = def.Id,
                    CommandId = def.Id,
                    Name = def.NameReadable,
                    SearchName = def.SearchTags == null ? def.NameReadable : $"{def.NameReadable} {def.SearchTags}",
                    EnumName = def.Name,
                    Help = def.Help ?? "",
                    Order = order++,
                });

                // Variant names are authored to be self-descriptive ("Cast spell (without target)",
                // "Movement: Idle"), so the tile shows just the variant name; the base command name
                // stays searchable.
                for (var i = 0; i < def.Variants.Count; ++i)
                {
                    var v = def.Variants[i];
                    AddItem(new DbScriptCommandItem(group, favourites!.IsFavourite($"{def.Name}/{v.NameReadable}"), SetFavourite)
                    {
                        Key = def.Id | ((long)(i + 1) << 32),
                        CommandId = def.Id,
                        Name = v.NameReadable,
                        SearchName = $"{v.NameReadable} {def.NameReadable} {def.SearchTags} {v.SearchTags}",
                        EnumName = $"{def.Name}/{v.NameReadable}",
                        Help = v.Description ?? def.Help ?? "",
                        Order = order++,
                    });
                }
            }
        }

        private void AddItem(DbScriptCommandItem item)
        {
            AllItems.Add(item);
            Items.Add(item);
        }

        private void SetFavourite(DbScriptCommandItem item, bool @is) => favourites?.SetFavourite(item.EnumName, @is);

        private async Task FilterAndSort(string? text, CancellationTokenSource tokenSource, CancellationToken cancellationToken)
        {
            while (currentToken != null)
            {
                await Task.Delay(50, cancellationToken);
                currentToken = tokenSource;
            }

            var lower = text?.ToLower();
            long? searchId = null;
            if (long.TryParse(text, out var textInt))
                searchId = textInt;

            // filtering on a separate thread, so that UI doesn't lag
            await Task.Run(() =>
            {
                visibleCount = 0;
                foreach (var item in AllItems)
                {
                    if (searchId.HasValue && searchId.Value == item.Key)
                        item.Score = 101;
                    else if (string.IsNullOrEmpty(lower))
                    {
                        item.Score = 100;
                    }
                    else if (item.Name.Equals(lower, StringComparison.InvariantCultureIgnoreCase))
                    {
                        item.Score = 101;
                    }
                    else
                    {
                        int indexOf = item.SearchName.IndexOf(lower, StringComparison.InvariantCultureIgnoreCase);
                        bool contains = indexOf != -1;
                        bool isFullWord = false;
                        if (contains)
                        {
                            isFullWord = true;
                            if (indexOf > 0 && item.SearchName[indexOf - 1] != ' ')
                                isFullWord = false;
                            indexOf += lower.Length;
                            if (indexOf < item.SearchName.Length && item.SearchName[indexOf] != ' ')
                                isFullWord = false;
                        }
                        var score = FuzzySharp.Fuzz.WeightedRatio(item.SearchName, lower);
                        item.Score = contains ? (Math.Max(score, isFullWord ? 85 : 62)) : score;
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

        private DbScriptCommandItem? FindExactMatching()
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

        public List<DbScriptCommandItem> AllItems { get; } = new();
        public ObservableCollectionExtended<DbScriptCommandItem> Items { get; } = new();

        private string searchBox = "";
        public string SearchBox
        {
            get => searchBox;
            set => SetProperty(ref searchBox, value);
        }

        private DbScriptCommandItem? selectedItem;
        public DbScriptCommandItem? SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
                _accept?.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand<DbScriptCommandItem> ToggleFavouriteCommand { get; private set; } = null!;
        public ICommand Cancel { get; private set; } = null!;
        private DelegateCommand _accept { get; set; } = null!;
        public ICommand Accept => _accept;
        public int DesiredWidth { get; init; } = 750;
        public int DesiredHeight { get; init; } = 650;
        // Tile width of the grouped wrap panel (actor pickers use wider tiles for longer labels).
        public double ItemWidth { get; init; } = 180;
        public string Title { get; }
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class DbScriptCommandItem : ObservableBase
    {
        private readonly Action<DbScriptCommandItem, bool>? makeFavourite;
        private int score = 100;
        private readonly string searchName = "";
        private bool isFavourite;
        private readonly string originalGroup;

        public DbScriptCommandItem(string group, bool isFavourite, Action<DbScriptCommandItem, bool>? makeFavourite)
        {
            this.makeFavourite = makeFavourite;
            Group = originalGroup = group;
            this.isFavourite = isFavourite;
            if (isFavourite)
                Group = "Favourites";
        }

        public bool ShowItem => Score > 61;

        public int Score
        {
            get => score;
            set
            {
                SetProperty(ref score, value);
                RaisePropertyChanged(nameof(ShowItem));
            }
        }

        public bool IsFavourite
        {
            get => isFavourite;
            set
            {
                if (makeFavourite == null)
                    return;
                Group = value ? "Favourites" : originalGroup;
                RaisePropertyChanged(nameof(Group));
                SetProperty(ref isFavourite, value);
                makeFavourite(this, value);
            }
        }

        // Encoded picker key: command id | ((variantIndex+1) << 32), or a negative sentinel.
        public long Key { get; init; }
        public uint CommandId { get; init; }
        // Stable favourite-persistence key (SCRIPT_COMMAND_* name, "/variant" suffixed).
        public string EnumName { get; init; } = "";
        public string Name { get; init; } = "";

        public string SearchName
        {
            get => searchName;
            init => searchName = value.ToLower();
        }

        public string Help { get; init; } = "";
        public bool IsWait { get; init; }
        public string Group { get; private set; }
        public int Order { get; init; }
    }
}
