using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.LootEditor.Picker.ViewModels;

public partial class LootPickerViewModel : ObservableBase, IDialog
{
    public IItemStore ItemStore { get; }
    public IParameter<long> ItemParameter { get; }
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMainThread mainThread;
    private readonly LootSourceType lootType;

    [Notify] private bool flattenReferences;
    
    [Notify] private string searchText = "";

    public ITableMultiSelection MultiSelection { get; } = new TableMultiSelection();
    
    [AlsoNotify(nameof(FocusedRow))] 
    [Notify] private VerticalCursor focusedRowIndex = VerticalCursor.None;
    
    public LootGroupViewModel? FocusedGroup =>
        focusedRowIndex.GroupIndex >= 0 && focusedRowIndex.GroupIndex < Items.Count ?
            Items[focusedRowIndex.GroupIndex] : null;
    
    public ItemViewModel? FocusedRow =>
        focusedRowIndex.GroupIndex >= 0 && focusedRowIndex.GroupIndex < Items.Count &&
        focusedRowIndex.RowIndex >= 0 && focusedRowIndex.RowIndex < Items[focusedRowIndex.GroupIndex].Rows.Count ?
            Items[focusedRowIndex.GroupIndex].FilteredItems[focusedRowIndex.RowIndex] : null;

    private List<LootGroupViewModel> allItems = new();
    public ObservableCollection<LootGroupViewModel> Items { get; } = new();
    
    public List<TableTableColumnHeader> Columns { get; } = new()
    {
        new ("Item or currency", 60),
        new ("Name", 300),
        new ("Chance", 60),
        new ("Loot mode", 90),
        new ("Group id", 50),
        new ("Min count or ref", 80),
        new ("Max count", 80)
    };
    
    public LootPickerViewModel(IDatabaseProvider databaseProvider,
        IParameterFactory parameterFactory,
        IItemStore itemStore,
        IMainThread mainThread,
        LootSourceType type)
    {
        ItemStore = itemStore;
        ItemParameter = parameterFactory.Factory("ItemParameter");
        this.databaseProvider = databaseProvider;
        this.mainThread = mainThread;
        this.lootType = type;
        Accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        });
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        Load().ListenErrors();
        
        On<string>(() => SearchText, PerformSearch);
        On<bool>(() => FlattenReferences, PerformFlattenReference);
    }

    public override void Dispose()
    {
        base.Dispose();
        GC.Collect();
    }

    private void PerformFlattenReference(bool on)
    {
        PerformSearch(searchText);
    }

    private void PerformSearch(string text)
    {
        var currentlyFocusedRow = FocusedRow;
        Items.RemoveAll();
        
        foreach (var x in allItems)
            x.PerformSearch(text);

        bool isSearchNumber = long.TryParse(text, out var searchNumber);
        
        if (string.IsNullOrWhiteSpace(text))
            Items.AddRange(allItems);
        else
        {
            foreach (var item in allItems)
            {
                if (isSearchNumber && item.LootEntry.Contains(searchText) ||
                    item.Name != null && item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    item.PerformSearch(""); // workaround to add all items, because we matched the loot entry
                
                if (item.FilteredItems.Count > 0)
                    Items.Add(item);
            }
        }

        // delay, because we need to wait an extra frame for the scroll viewer to update its size
        mainThread.Delay(() =>
        {
            FocusedRowIndex = VerticalCursor.None;
            for (var groupIndex = 0; groupIndex < Items.Count; groupIndex++)
            {
                var lootGroup = Items[groupIndex];
                for (var itemIndex = 0; itemIndex < lootGroup.FilteredItems.Count; itemIndex++)
                {
                    var item = lootGroup.FilteredItems[itemIndex];
                    if (item == currentlyFocusedRow)
                    {
                        FocusedRowIndex = new VerticalCursor(groupIndex, itemIndex);
                    }
                }
            }
            MultiSelection.Clear();
            if (FocusedRow != null)
                MultiSelection.Add(FocusedRowIndex);
        }, TimeSpan.FromMilliseconds(1));
    }

    private async Task Load()
    {
        var names = await databaseProvider.GetLootTemplateName(lootType);
        var allLoot = await databaseProvider.GetLoot(lootType);
        var allReferences = lootType == LootSourceType.Reference ? allLoot : await databaseProvider.GetLoot(LootSourceType.Reference);
        var referenceNames = lootType == LootSourceType.Reference ? names : await databaseProvider.GetLootTemplateName(lootType);
        
        Dictionary<uint, List<ILootEntry>> allRefsByEntry = allReferences.GroupBy(x => x.Entry).ToDictionary(x => x.Key, x => x.ToList());
        Dictionary<uint, ILootTemplateName> namesByEntry = names.ToDictionary(x => x.Entry, x => x);
        Dictionary<uint, ILootTemplateName> referenceNamesByEntry = referenceNames.ToDictionary(x => x.Entry, x => x);

        HashSet<uint> visited = new();

        Dictionary<(uint, int), ItemViewModel> referenceItemByEntry = new ();
        void Dfs(LootGroupViewModel group)
        {
            visited.Clear();
            for (int i = 0; i < group.AllItemsFlatten.Count; ++i)
            {
                var item = group.AllItemsFlatten[i];
                if (!item.IsReference)
                    continue;

                var refId = item.ReferenceId;
                if (!visited.Add(refId))
                    continue;

                if (referenceNamesByEntry.TryGetValue(refId, out var referenceName) && referenceName.DontLoadRecursively)
                    continue;
                
                if (!allRefsByEntry.TryGetValue(refId, out var referenceEntries))
                    continue;

                foreach (var entry in referenceEntries)
                {
                    if (!referenceItemByEntry.TryGetValue((entry.Entry, entry.ItemOrCurrencyId), out var vm))
                    {
                        ILootTemplateName? name = null;
                        if (entry.Reference > 0)
                            referenceNamesByEntry.TryGetValue(entry.Reference, out name);

                        vm = referenceItemByEntry[(entry.Entry, entry.ItemOrCurrencyId)] = new ItemViewModel(this, entry, name);
                    }
                    group.AllItemsFlatten.Add(vm);
                }
            }
        }
        
        LootGroupViewModel? group = null;
        foreach (var entry in allLoot)
        {
            if (group == null || group.LootEntry != entry.Entry)
            {
                namesByEntry.TryGetValue(entry.Entry, out var name);
                group = new LootGroupViewModel(this, entry.Entry, name?.Name);
                allItems.Add(group);
            }
            
            ILootTemplateName? refName = null;
            if (entry.Reference > 0)
                referenceNamesByEntry.TryGetValue(entry.Reference, out refName);
            
            group.AllItemsRaw.Add(new ItemViewModel(this, entry, refName));
            group.AllItemsFlatten.Add(new ItemViewModel(this, entry, refName));
            Dfs(group);
        }

        PerformSearch(searchText);
    }

    public int DesiredWidth => 850;
    public int DesiredHeight => 700;
    public string Title => "Pick loot " + lootType;
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}