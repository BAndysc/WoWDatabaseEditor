using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using AvaloniaStyles.Controls.FastTableView;
using Newtonsoft.Json;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.LootEditor.Configuration;
using WDE.LootEditor.DataLoaders;
using WDE.LootEditor.Models;
using WDE.LootEditor.QueryGenerator;
using WDE.LootEditor.Services;
using WDE.LootEditor.Solution;
using WDE.LootEditor.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.Editor.ViewModels;

public partial class LootEditorViewModel : ObservableBase, ISolutionItemDocument
{
    public IDbcStore DbcStore { get; }
    public IItemStore ItemStore { get; }
    public IParameterFactory ParameterFactory { get; }
    public ILootEditorFeatures LootEditorFeatures { get; }
    public IDatabaseProvider DatabaseProvider { get; }
    private readonly LootSolutionItem solutionItem;
    private readonly IHistoryManager historyManager;
    private readonly ILootLoader lootLoader;
    private readonly IConditionEditService conditionEditService;
    private readonly ILootEditorPreferences preferences;
    private readonly ILootQueryGenerator queryGenerator;
    private readonly ILootUserQuestionsService userQuestionsService;
    private readonly IMessageBoxService messageBoxService;
    private readonly ITaskRunner taskRunner;
    private readonly IStatusBar statusBar;
    private readonly IParameterPickerService parameterPickerService;
    private readonly IWoWHeadLootImporter woWHeadLootImporter;
    private readonly IFileSystem fileSystem;
    private readonly IMySqlExecutor mySqlExecutor;

    public ICommand Undo { get; }
    public ICommand Redo { get; }
    public IHistoryManager History => historyManager;
    public HistoryHandler HistoryHandler { get; } = new();
    public bool IsModified => !History.IsSaved;
    public string Title { get; }
    public ICommand Copy { get; }
    public ICommand Cut { get; }
    public ICommand Paste { get; }
    public IAsyncCommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ImageUri? Icon => new ImageUri("Icons/document_loot.png");

    public ObservableCollection<LootGroup> Loots { get; } = new();
    public List<TableTableColumnHeader> LootColumns { get; } = new();

    [AlsoNotify(nameof(FocusedRow))] 
    [Notify] private VerticalCursor focusedRowIndex = VerticalCursor.None;
        
    [Notify] private int focusedCellIndex = -1;
    public ITableMultiSelection MultiSelection { get; } = new TableMultiSelection();
    
    [Notify] private string searchText = "";

    public LootItemViewModel? FocusedRow =>
        focusedRowIndex.GroupIndex >= 0 && focusedRowIndex.GroupIndex < Loots.Count &&
        focusedRowIndex.RowIndex >= 0 && focusedRowIndex.RowIndex < Loots[focusedRowIndex.GroupIndex].Rows.Count ?
            Loots[focusedRowIndex.GroupIndex].LootItems[focusedRowIndex.RowIndex] : null;

    public LootSolutionItem SolutionItem => solutionItem;

    public AsyncAutoCommand<LootItemViewModel> EditConditionsCommand { get; }
    
    public DelegateCommand<LootGroup> CollapseExpandCommand { get; }
    
    public DelegateCommand<LootGroup> CollapseExpandAllCommand { get; }
    
    public ICommand DeleteSelectedLootItemsCommand { get; }

    private int indexOfReferenceCell = -1;

    public ObservableCollection<LootQuickButtonViewModel> Buttons { get; } = new();
    
    public DelegateCommand OpenLootConfigurationCommand { get; }
    
    public DelegateCommand<LootGroup> ConvertGroupToButtonCommand { get; }
    
    public AsyncAutoCommand<LootGroup> RemoveLootCommand { get; }
    
    public AsyncAutoCommand AddNewLootCommand { get; }
    
    public AsyncAutoCommand<LootGroup> EditGroupNameCommand { get; }
    
    public AsyncAutoCommand<LootGroup> OpenCrossReferencesCommand { get; }
    
    public IParameter<long> ItemParameter { get; internal set; } = Parameter.Instance;
    
    private Dictionary<(LootSourceType, LootEntry), string> lootTemplateNames = new();
    
    internal LootEditorViewModel(LootSolutionItem solutionItem,
        IHistoryManager historyManager,
        ILootLoader lootLoader,
        IConditionEditService conditionEditService,
        ILootEditorPreferences preferences,
        ILootQueryGenerator queryGenerator,
        IDbcStore dbcStore,
        ISolutionItemNameRegistry solutionItemNameRegistry,
        ILootUserQuestionsService userQuestionsService,
        IMessageBoxService messageBoxService,
        ITaskRunner taskRunner,
        IStatusBar statusBar,
        IItemStore itemStore,
        IParameterFactory parameterFactory,
        IParameterPickerService parameterPickerService,
        ILootEditorFeatures lootEditorFeatures,
        ILootPickerService lootPickerService,
        IWoWHeadLootImporter woWHeadLootImporter,
        IFileSystem fileSystem,
        IMySqlExecutor mySqlExecutor,
        IConfigureService configureService,
        IDatabaseProvider databaseProvider,
        IWindowManager windowManager,
        IClipboardService clipboardService,
        ILootCrossReferencesService crossReferencesService)
    {
        DbcStore = dbcStore;
        ItemStore = itemStore;
        ParameterFactory = parameterFactory;
        LootEditorFeatures = lootEditorFeatures;
        DatabaseProvider = databaseProvider;
        this.solutionItem = solutionItem;
        this.historyManager = historyManager;
        historyManager.AddHandler(HistoryHandler);
        this.lootLoader = lootLoader;
        this.conditionEditService = conditionEditService;
        this.preferences = preferences;
        this.queryGenerator = queryGenerator;
        this.userQuestionsService = userQuestionsService;
        this.messageBoxService = messageBoxService;
        this.taskRunner = taskRunner;
        this.statusBar = statusBar;
        this.parameterPickerService = parameterPickerService;
        this.woWHeadLootImporter = woWHeadLootImporter;
        this.fileSystem = fileSystem;
        this.mySqlExecutor = mySqlExecutor;

        Undo = historyManager.UndoCommand();
        Redo = historyManager.RedoCommand();

        Save = new AsyncAutoCommand(async () =>
        {
            await taskRunner.ScheduleTask($"Export {Title} to database",
                async progress =>
                {
                    try
                    {
                        var query = (await GenerateQuery()).QueryString;
                        await mySqlExecutor.ExecuteSql(query);
                        History.MarkAsSaved();
                        SetAsSaved();
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Success,"Saved to database"));
                        progress.ReportFinished();
                    }
                    catch (LootDuplicateKeysException e)
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Error")
                            .SetMainInstruction("Couldn't generate the SQL")
                            .SetContent(e.Message)
                            .WithOkButton(false)
                            .Build());
                        progress.ReportFail();
                    }
                    catch (IMySqlExecutor.QueryFailedDatabaseException e)
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Couldn't apply SQL: " + e.Message));
                        await userQuestionsService.NotifyCouldNotApplySql(e);
                        throw;
                    }
                });
        });

        CollapseExpandCommand = new DelegateCommand<LootGroup>(g =>
        {
            g.IsExpanded = !g.IsExpanded;
        });

        CollapseExpandAllCommand = new DelegateCommand<LootGroup>(g =>
        {
            var newValue = !g.IsExpanded;
            foreach (var o in Loots)
            {
                o.IsExpanded = newValue;
            }
        });

        LootColumns.Add(new TableTableColumnHeader("Item or currency"){Width = 80});
        LootColumns.Add(new TableTableColumnHeader("Name"){Width = 200});
        LootColumns.Add(new TableTableColumnHeader("Chance"){Width = 70});
        if (LootEditorFeatures.HasLootModeField)
            LootColumns.Add(new TableTableColumnHeader("Loot mode"){Width = 100});
        LootColumns.Add(new TableTableColumnHeader("Group"){Width = 70});
        indexOfReferenceCell = LootColumns.Count;
        LootColumns.Add(new TableTableColumnHeader("Min count or ref"){Width = 100});
        LootColumns.Add(new TableTableColumnHeader("Max count"){Width = 100});
        LootColumns.Add(new TableTableColumnHeader(lootEditorFeatures.HasConditionId ? "Condition id" : "Conditions"){Width = 120});
        if (LootEditorFeatures.HasCommentField(solutionItem.Type))
            LootColumns.Add(new TableTableColumnHeader("Comment"){Width = 300});
        if (LootEditorFeatures.HasPatchField)
        {
            LootColumns.Add(new TableTableColumnHeader("Min Patch"){Width = 70});
            LootColumns.Add(new TableTableColumnHeader("Max Patch"){Width = 70});
        }
        
        foreach (var column in LootColumns)
            if (preferences.TryGetColumnWidth(column.Header, out var width))
                column.Width = width;

        foreach (var column in LootColumns)
            column.ToObservable(x => x.Width)
                .SubscribeAction(_ => UpdateColumnWidths());

        Copy = new DelegateCommand(() =>
        {
            List<LootModel> entries = new();
            foreach (var selected in MultiSelection.All())
            {
                var vm = Loots[selected.GroupIndex].LootItems[selected.RowIndex];
                entries.Add(vm.ToModel());
            }

            if (entries.Count > 0)
                clipboardService.SetText(JsonConvert.SerializeObject(entries));
        });

        Cut = new DelegateCommand(() =>
        {
            using var _ = HistoryHandler.WithinBulk("Cut");
            Copy.Execute(null);
            foreach (var selected in MultiSelection.AllReversed().ToList())
                Loots[selected.GroupIndex].LootItems.RemoveAt(selected.RowIndex);
        });

        Paste = new AsyncCommand(async () =>
        {
            if (Loots.Count == 0)
                return;
            
            var text = await clipboardService.GetText();
            List<LootModel> entries = new List<LootModel>();
            try
            {
                entries = JsonConvert.DeserializeObject<List<LootModel>>(text)!;
            }
            catch (Exception e)
            {
            }
            
            if (entries.Count == 0)
                return;

            int i = 0;
            if (!(focusedRowIndex.GroupIndex >= 0 &&
                  focusedRowIndex.GroupIndex < Loots.Count &&
                  focusedRowIndex.RowIndex >= 0 &&
                  focusedRowIndex.RowIndex < Loots[focusedRowIndex.GroupIndex].Rows.Count))
            {
                focusedRowIndex = new VerticalCursor(0, 0);
            }
            else
            {
                i++; // to paste below
            }
            
            MultiSelection.Clear();
            using var _ = HistoryHandler.WithinBulk("Paste");
            var group = Loots[focusedRowIndex.GroupIndex];
            foreach (var x in entries)
            {
                var vm = new LootItemViewModel(group, x);
                MultiSelection.Add(new VerticalCursor(focusedRowIndex.GroupIndex, focusedRowIndex.RowIndex + i));
                group.LootItems.Insert(focusedRowIndex.RowIndex + i++, vm);
            }
        });
        
        OpenLootConfigurationCommand = new DelegateCommand(configureService.ShowSettings<LootEditorConfiguration>);

        ConvertGroupToButtonCommand = new DelegateCommand<LootGroup>(group =>
        {
            string? name = "Add";
            string? iconPath = null;
            var currentButtons = preferences.Buttons.Value.ToList();
            if (group.LootItems.FirstOrDefault(x => !x.IsReference) is { } firstNonReference)
            {
                name = "Add " + firstNonReference.GetItemOrCurrencyName();
                uint? fileId = null;
                if (firstNonReference.ItemOrCurrencyId.Value >= 0)
                {
                    var item = itemStore.GetItemById((uint)firstNonReference.ItemOrCurrencyId.Value);
                    if (item != null && item.UsesFileId)
                        fileId = item.InventoryIconFileDataId!.Value;
                }
                else
                {
                    var currency = itemStore.GetCurrencyTypeById((uint)-firstNonReference.ItemOrCurrencyId.Value);
                    if (currency != null && currency.UsesFileId)
                        fileId = currency.InventoryIconFileId!.Value;
                }

                if (fileId.HasValue)
                {
                    var file = fileSystem.ResolvePhysicalPath("/common/item_icons/" + fileId.Value + ".png");
                    if (file.Exists)
                        iconPath = file.FullName;
                }
            }
            currentButtons.Add(new LootButtonDefinition()
            {
                ButtonText = iconPath == null ? name : null,
                ButtonToolTip = name,
                Icon = iconPath,
                CustomItems = group.LootItems.Select(item => new LootButtonDefinition.CustomLootItemDefinition()
                {
                    ItemOrCurrencyId = (int)item.ItemOrCurrencyId.Value,
                    Chance = item.Chance.Value,
                    LootMode = (int)item.LootMode.Value,
                    GroupId = (int)item.GroupId.Value,
                    MinCountOrRef = (int)item.MinCountOrRef.Value,
                    MaxCount = (int)item.MaxCount.Value,
                    Conditions = item.Conditions?.Select(x => new AbstractCondition(x)).ToList()
                }).ToList()
            });
            this.preferences.SaveButtons(currentButtons);
        });
        
        EditConditionsCommand = new AsyncAutoCommand<LootItemViewModel>(async loot =>
        {
            var newConditions = await conditionEditService.EditConditions(lootEditorFeatures.GetConditionSourceTypeFor(loot.Parent.LootSourceType), loot.Conditions);
            if (newConditions != null)
                loot.Conditions = newConditions.ToList();
        });

        AddNewLootCommand = new AsyncAutoCommand(async () =>
        {
            var picker = await lootPickerService.PickLoot(solutionItem.Type);
            if (!picker.HasValue)
                return;

            using var _ = HistoryHandler.WithinBulk("Add loot");
            await LoadLoot(solutionItem.Type, new LootEntry(picker.Value));
            await LoadRecursively();
        }, () => solutionItem.Type.CanUpdateSourceLootEntry() &&
                 Loots.Count(x => x.LootSourceType == solutionItem.Type) < lootEditorFeatures.GetMaxLootEntryForType(solutionItem.Type, solutionItem.Difficulty));
        
        DeleteSelectedLootItemsCommand = new AsyncAutoCommand(async () =>
        {
            List<LootItemViewModel> optionsToDelete = new List<LootItemViewModel>();
            foreach (var cursor in MultiSelection.AllReversed())
            {
                optionsToDelete.Add(Loots[cursor.GroupIndex].LootItems[cursor.RowIndex]);
            }

            if (!await AskToSave(null, optionsToDelete, null))
                return;
            
            using IDisposable disposable = HistoryHandler.WithinBulk("Delete loot items");
            foreach (var cursor in MultiSelection.AllReversed())
            {
                Loots[cursor.GroupIndex].LootItems.RemoveAt(cursor.RowIndex);
            }
            await LoadRecursively();
            MultiSelection.Clear();
        });

        RemoveLootCommand = new AsyncAutoCommand<LootGroup>(async group =>
        {
            if (!await AskToSave(null, null, new List<LootEntry>() { group.LootEntry }))
                return;

            using var _ = HistoryHandler.WithinBulk("Remove loot");
            Loots.Remove(group);
            await LoadRecursively();
        });
        
        EditGroupNameCommand = new AsyncAutoCommand<LootGroup>(async group =>
        {
            using var vm = new EditLootGroupDialogViewModel(lootEditorFeatures, group);
            if (await windowManager.ShowDialog(vm))
            {
                using var _ = HistoryHandler.WithinBulk("Edit group");
                var oldName = group.GroupName;
                var newName = string.IsNullOrWhiteSpace(vm.Name) ? null : vm.Name;
                var oldFlag = group.DontLoadRecursively;
                var newFlag = vm.DontLoadRecursively;
                HistoryHandler.DoAction(new AnonymousHistoryAction("Edit group", () =>
                {
                    group.GroupName = oldName;
                    group.DontLoadRecursively = oldFlag;
                }, () =>
                {
                    group.GroupName = newName;
                    group.DontLoadRecursively = newFlag;
                }));
            }
        }, group => lootEditorFeatures.LootGroupHasName(group?.LootSourceType ?? LootSourceType.Creature));

        OpenCrossReferencesCommand = new AsyncAutoCommand<LootGroup>(async group =>
        {
            await crossReferencesService.OpenCrossReferencesDialog(group.LootSourceType, group.LootEntry);
        });

        Loots.CollectionChanged += OptionsChanged;
        
        On<VerticalCursor>(() => FocusedRowIndex, _ => UpdateFocusedReferenceLoot());
        On<int>(() => FocusedCellIndex, _ => UpdateFocusedReferenceLoot());
        
        historyManager.ToObservable(x => x.IsSaved)
            .SubscribeAction(_ => RaisePropertyChanged(nameof(IsModified)));

        AutoDispose(preferences.Buttons.SubscribeAction(definition =>
        {
            Buttons.Clear();
            foreach (var def in definition)
            {
                if ((def.ButtonType is LootButtonType.ImportFromWowHead ||
                     def.ButtonType is LootButtonType.ImportQuestItemsFromWowHead) &&
                    ((solutionItem.Type is not LootSourceType.Creature and not LootSourceType.GameObject) ||
                    !woWHeadLootImporter.IsAvailable))
                    continue;

                if (def.ButtonType is LootButtonType.AddCurrency &&
                    !LootEditorFeatures.ItemCanBeCurrency)
                    continue;
                
                Buttons.Add(new LootQuickButtonViewModel(def, CreateCommandForButton(def).WrapMessageBox<Exception, LootGroup>(messageBoxService)));
            }
        }));
        
        Title = $"{solutionItemNameRegistry.GetName(solutionItem)} ({solutionItem.Entry})";
        IsInitialLoading = true;
    }

    public Task BeginLoad()
    {
        return taskRunner.ScheduleTask("Loading loot", Load);
    }

    private IAsyncCommand<LootGroup> CreateCommandForButton(LootButtonDefinition definition)
    {
        switch (definition.ButtonType)
        {
            case LootButtonType.AddItemSet:
                return new AsyncAutoCommand<LootGroup>(group =>
                {
                    using var bulk = HistoryHandler.WithinBulk("Add items");
                    if (definition.CustomItems != null)
                    {
                        foreach (var def in definition.CustomItems)
                        {
                            var vm = group.AddNewLootItem(def.ItemOrCurrencyId);
                            vm.Chance.SetValue(def.Chance).ListenErrors();
                            vm.LootMode.SetValue(def.LootMode).ListenErrors();
                            vm.GroupId.SetValue(def.GroupId).ListenErrors();
                            vm.MinCountOrRef.SetValue(def.MinCountOrRef).ListenErrors();
                            vm.MaxCount.SetValue(def.MaxCount).ListenErrors();
                            vm.Comment.SetValue(def.Comment ?? "").ListenErrors();
                            if (def.Conditions != null)
                                vm.Conditions = def.Conditions.ToList();
                        }
                    }

                    return Task.CompletedTask;
                });
            case LootButtonType.AddItem:
                return new AsyncAutoCommand<LootGroup>(AddItem);
            case LootButtonType.AddCurrency:
                return new AsyncAutoCommand<LootGroup>(AddCurrency);
            case LootButtonType.AddReference:
                return new AsyncAutoCommand<LootGroup>(AddReference);
            case LootButtonType.ImportQuestItemsFromWowHead:
                return new AsyncAutoCommand<LootGroup>(async group => await WoWHeadImport(group, true));
            case LootButtonType.ImportFromWowHead:
                return new AsyncAutoCommand<LootGroup>(async group => await WoWHeadImport(group, false));
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new AsyncAutoCommand<LootGroup>(_ => Task.CompletedTask, _ => false);
    }

    private async Task AddItem(LootGroup group)
    {
        var items = await parameterPickerService.PickMultiple(ParameterFactory.Factory("ItemParameter"));
        if (items.Count == 0)
            return;
        
        using var _ = HistoryHandler.WithinBulk("Add items");
        
        foreach (var item in items)
            group.AddNewLootItem(item);
    }

    private async Task AddCurrency(LootGroup group)
    {
        var items = await parameterPickerService.PickMultiple(ParameterFactory.Factory("CurrencyTypeParameter"));
        if (items.Count == 0)
            return;
        
        using var _ = HistoryHandler.WithinBulk("Add currency");

        foreach (var item in items)
            group.AddNewLootItem(-item);
    }
    
    private async Task AddReference(LootGroup group)
    {
        var items = await parameterPickerService.PickMultiple(ParameterFactory.Factory("LootReferenceParameter"));
        if (items.Count == 0)
            return;
        
        using var _ = HistoryHandler.WithinBulk("Add references");
        
        foreach (var item in items)
        {
            var vm = group.AddNewLootItem(LootEditorFeatures.ItemCanBeCurrency ? -item : item);
            await vm.MinCountOrRef.SetValue(-item);
        }
    }

    private async Task WoWHeadImport(LootGroup group, bool onlyQuestItems)
    {
        IList<AbstractLootEntry> loot;
        if (solutionItem.Type == LootSourceType.Creature)
            loot = await woWHeadLootImporter.GetCreatureLoot(solutionItem.Entry, onlyQuestItems);
        else if (solutionItem.Type == LootSourceType.GameObject)
            loot = await woWHeadLootImporter.GetGameObjectLoot(solutionItem.Entry, onlyQuestItems);
        else
            throw new Exception($"Loot type ({solutionItem.Type}) not supported");

        using var _ = HistoryHandler.WithinBulk("Import from WoWHead");
        foreach (var item in loot)
            group.AddNewLootItem(item);
    }

    private void UpdateColumnWidths()
    {
        preferences.SaveColumnsWidth(LootColumns.Select(x => (x.Header, x.Width)));
    }

    private void UpdateFocusedReferenceLoot()
    {
        LootEntry? focusedReferenceId = default;
        if (focusedCellIndex == indexOfReferenceCell)
        {
            if (focusedRowIndex.GroupIndex >= 0 &&
                focusedRowIndex.GroupIndex < Loots.Count &&
                focusedRowIndex.RowIndex >= 0 &&
                focusedRowIndex.RowIndex < Loots[focusedRowIndex.GroupIndex].Rows.Count)
            {
                var focusedLootItem = Loots[focusedRowIndex.GroupIndex].LootItems[focusedRowIndex.RowIndex];
                if (focusedLootItem.IsReference)
                    focusedReferenceId = new LootEntry(focusedLootItem.ReferenceEntry);
            }
        }
        else
        {
            
        }

        foreach (var group in Loots)
        {
            group.IsFocused = group.LootSourceType == LootSourceType.Reference && focusedReferenceId == group.LootEntry;
        }
    }

    private void OptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AddNewLootCommand.RaiseCanExecuteChanged();
        
        if (e.Action == NotifyCollectionChangedAction.Reset)
            throw new Exception("Reset is not supported, because it is not known what are old items.");
        
        if (e.OldItems != null)
        {
            int i = 0;
            foreach (LootGroup item in e.OldItems)
            {
                item.OnReferenceLootChanged -= ItemOnReferenceLootChanged;
                item.OnBeforeReferenceLootChanged -= ItemsOnBeforeReferenceLootChanged;

                var index = e.OldStartingIndex + i;
                
                HistoryHandler.PushAction(new AnonymousHistoryAction("Remove menu", () =>
                {
                    Loots.Insert(index, item);
                }, () =>
                {
                    Loots.Remove(item);
                }));
                i++;
            }
        }

        if (e.NewItems != null)
        {
            int i = 0;
            foreach (LootGroup item in e.NewItems)
            {
                item.OnReferenceLootChanged += ItemOnReferenceLootChanged;
                item.OnBeforeReferenceLootChanged += ItemsOnBeforeReferenceLootChanged;
                var index = e.NewStartingIndex + i;
                
                HistoryHandler.PushAction(new AnonymousHistoryAction("Add menu", () =>
                {
                    Loots.Remove(item);
                }, () =>
                {
                    Loots.Insert(index, item);
                }));
                i++;
            }
        }
    }
    
    private bool IgnoreLoot(LootSourceType type, LootEntry id) => lootToNeverLoadAgain.Contains((type, id));
    
    public bool HasLootEntry(LootSourceType type, LootEntry id) => Loots.Any(x => x.LootSourceType == type && x.LootEntry == id);
    
    public bool TryGetLootName(LootSourceType type, LootEntry id, out string? name) => lootTemplateNames.TryGetValue((type, id), out name);

    public void InvalidateReferenceLootNames()
    {
        foreach (var group in Loots)
        {
            foreach (var item in group.LootItems)
            {
                if (item.IsReference)
                {
                    item.InvalidateName();
                }
            }
        }
    }
    
    private async Task ItemsOnBeforeReferenceLootChanged(LootItemViewModel sender, OnBeforeChangedEventArgs<long> e)
    {
        var @new = e.New <= 0 ? 0u : (uint)e.New;
        var old = e.Old <= 0 ? 0u : (uint)e.Old;
        
        if (!await AskToSave(new (){(sender, @new)}, null, null))
            e.Cancel = true;
    }
    
    private async Task ItemOnReferenceLootChanged(LootGroup obj, EventArgs _)
    {
        if (History.IsUndoing)
            return;
        
        await LoadRecursively();
    }

    public async Task UpdateSelectedCells(string text)
    {
        System.IDisposable disposable = new EmptyDisposable();
        if (MultiSelection.MoreThanOne)
            disposable = HistoryHandler.WithinBulk("Bulk edit property");
        foreach (var selected in MultiSelection.All())
        {
            var cell = Loots[selected.GroupIndex].Rows[selected.RowIndex].CellsList[focusedCellIndex];
            if (cell is LootItemParameterCell<long> longCell)
                await longCell.UpdateFromStringAsync(text);
            else if (cell is LootItemParameterCell<string> stringCell)
                await stringCell.UpdateFromStringAsync(text);
            else
                cell.UpdateFromString(text);
        }
        disposable.Dispose();
    }
    
    /// <summary>
    /// Loads now, as opposed to BeginLoad which schedules loading via the task runner
    /// </summary>
    public async Task Load()
    {
        ItemParameter = ParameterFactory.Factory("ItemParameter");
        using var pause = HistoryHandler.Pause();
        try
        {
            var entries = await lootLoader.GetLootEntries(solutionItem.Type, solutionItem.Entry, solutionItem.Difficulty);
            foreach (var entry in entries)
            {
                if ((uint)entry > 0)
                    await LoadLoot(solutionItem.Type, entry);
            }

            await LoadRecursively();
        }
        catch (Exception e)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Error")
                .SetMainInstruction("Can't load loot")
                .SetContent(e.Message)
                .WithOkButton(true)
                .Build());
            throw;
        }
        finally
        {
            SetAsSaved();
            IsInitialLoading = false;
        }
    }

    private HashSet<(LootSourceType, LootEntry)> lootToNeverLoadAgain = new();
    
    public async Task LoadLoot(LootSourceType type, LootEntry entry)
    {
        if (HasLootEntry(type, entry))
            return;

        if (lootToNeverLoadAgain.Contains((type, entry)))
            return;
        
        var name = await lootLoader.GetLootTemplateName(type, entry);

        if (name != null &&
            (type != solutionItem.Type || (uint)entry != solutionItem.Entry) && // meaning the loot is dynamically loaded
            name.DontLoadRecursively)
        {
            lootTemplateNames[(type, entry)] = name.Name;
            lootToNeverLoadAgain.Add((type, entry));
            InvalidateReferenceLootNames();
            return;
        }
        
        var lootModels = await lootLoader.FetchLoot(type, entry);

        var lootGroup = new LootGroup(this, type, entry, name);
        lootGroup.LootItems.AddRange(lootModels.Select(o => new LootItemViewModel(lootGroup, o)));

        Loots.Add(lootGroup);
        
        ReorderOptionGroups();
    }
    
    private List<LootEntry> CollectUnreferencedLootRefs(List<(LootItemViewModel, long)>? minCountOrReferenceToChange, 
        List<LootItemViewModel>? lootItemsRemoved,
        ICollection<LootEntry>? removedRoots)
    {
        HashSet<LootGroup> rootAccessible = new HashSet<LootGroup>();
        List<LootEntry> danglingMenus = new List<LootEntry>();
        void Dfs(LootGroup group)
        {
            if (!rootAccessible.Add(group))
                return;
            
            foreach (var option in group.LootItems)
            {
                if (lootItemsRemoved?.Contains(option) ?? false)
                    continue;

                var replacement = minCountOrReferenceToChange?
                    .Where(x => x.Item1 == option)
                    .Select(x => (uint?)x.Item2)
                    .FirstOrDefault();

                var minCountOrRef = option.MinCountOrRef.Value;
                if (replacement.HasValue)
                    minCountOrRef = replacement.Value;
                
                if (minCountOrRef >= 0)
                    continue;

                if (FindLoot(LootSourceType.Reference, new LootEntry((uint)-minCountOrRef)) is not { } referencedGroup)
                    continue;

                Dfs(referencedGroup);
            }
        }

        var roots = Roots
            .Except(removedRoots ?? Enumerable.Empty<LootEntry>());

        foreach (var root in roots)
        {
            if (FindLoot(solutionItem.Type, root) is { } lootGroup)
                Dfs(lootGroup);
        }

        foreach (var group in Loots)
        {
            if (rootAccessible.Contains(group))
                continue;
            
            danglingMenus.Add(group.LootEntry);
        }

        return danglingMenus;
    }
    
    public async Task<bool> AskToSave(List<(LootItemViewModel, long)>? minCountOrReferenceToChange,
        List<LootItemViewModel>? lootItemsRemoved, List<LootEntry>? lootEntryRemoved)
    {
        var lootRefsToBeUnloaded = CollectUnreferencedLootRefs(minCountOrReferenceToChange, lootItemsRemoved, lootEntryRemoved);

        List<LootEntry> modifiedRefsToBeUnloaded = new List<LootEntry>();
        List<LootEntry> modifiedRootLootGroupToBeUnloaded = new List<LootEntry>();
        
        foreach (var lootEntry in lootRefsToBeUnloaded)
        {
            var vm = FindLoot(LootSourceType.Reference, lootEntry);
            if (vm != null && vm.IsModified)
            {
                modifiedRefsToBeUnloaded.Add(vm.LootEntry);
            }
        }
        
        foreach (var lootEntry in lootEntryRemoved ?? Enumerable.Empty<LootEntry>())
        {
            var vm = FindLoot(solutionItem.Type, lootEntry);
            if (vm != null && vm.IsModified)
            {
                modifiedRootLootGroupToBeUnloaded.Add(vm.LootEntry);
            }
        }

        if (modifiedRefsToBeUnloaded.Count > 0 || modifiedRootLootGroupToBeUnloaded.Count > 0)
        {
            var result = await userQuestionsService.AskToSave(solutionItem.Type,
                modifiedRefsToBeUnloaded,
                modifiedRootLootGroupToBeUnloaded);

            if (result == SaveDialogResult.Cancel)
                return false;

            if (result == SaveDialogResult.Save)
            {
                await Save.ExecuteAsync();
            }
            
            return true;
        }

        return true;
    }

    private Task? pendingLoadingTask;

    [Notify] [AlsoNotify(nameof(IsLoading))] private bool isInitialLoading;
    
    [Notify] [AlsoNotify(nameof(IsLoading))] private int loadingCounter = 0;
    
    public bool IsLoading => loadingCounter > 0 || isInitialLoading;

    public async Task LoadRecursivelyNoHistory()
    {
        var pause = HistoryHandler.Pause();
        try
        {
            await LoadRecursively();
        }
        finally
        {
            pause.Dispose();
        }
    }

    public async Task LoadRecursively()
    {
        async Task<bool> InnerLoadRecursively(Task? waitForTask)
        {
            if (waitForTask != null)
                await waitForTask;
            
            var allReferenceLoots = Loots
                .SelectMany(g => g.LootItems)
                .Where(l => l.IsReference)
                .Select(l => new LootEntry(l.ReferenceEntry))
                .Where(loot => !IgnoreLoot(LootSourceType.Reference, loot))
                .Distinct()
                .ToList();
            var looseReferences = allReferenceLoots
                .Where(subMenu => !HasLootEntry(LootSourceType.Reference, subMenu))
                .ToList();
            var notRequiredReferences = CollectUnreferencedLootRefs(null, null, null);

            if (looseReferences.Count == 0 && notRequiredReferences.Count == 0)
                return false;
        
            foreach (var menu in looseReferences)
            {
                await LoadLoot(LootSourceType.Reference, menu);
            }

            foreach (var menu in notRequiredReferences)
            {
                await UnloadLoot(LootSourceType.Reference, menu);
            }

            await InnerLoadRecursively(null);
            
            ReorderOptionGroups();
            
            return true;
        }
        
        try
        {
            LoadingCounter++;
            pendingLoadingTask = InnerLoadRecursively(pendingLoadingTask);
            await pendingLoadingTask;
        }
        finally
        {
            LoadingCounter--;
        }
    }
    
    private void ReorderOptionGroups()
    {
        bool isOrdered = true;
        int firstReference = -1;
        for (int i = 0; i < Loots.Count; ++i)
        {
            if (Loots[i].IsDynamicallyLoadedReference && firstReference == -1)
                firstReference = i;
            else if (!Loots[i].IsDynamicallyLoadedReference && i > firstReference)
                isOrdered = false;
        }
                
        if (isOrdered)
            return;
                
        List<LootGroup> rootLoots = new();
        List<LootGroup> dynamicReferences = new();
        foreach (var x in Loots)
        {
            if (x.IsDynamicallyLoadedReference)
                dynamicReferences.Add(x);
            else
                rootLoots.Add(x);
        }
        Loots.RemoveAll();
        Loots.AddRange(rootLoots);
        Loots.AddRange(dynamicReferences);
    }
    
    public void SetAsSaved()
    {
        foreach (var menu in Loots)
            menu.SetAsSaved();
    }
    
    private async Task UnloadLoot(LootSourceType type, LootEntry menu)
    {
        var group = FindLoot(type, menu);
        if (group != null)
        {
            group.LootItems.RemoveAll();
            Loots.Remove(group);
        }
    }

    public LootGroup? FindLoot(LootSourceType type, LootEntry entry) => Loots.FirstOrDefault(x => x.LootSourceType == type && x.LootEntry == entry);

    private IReadOnlyList<LootEntry> Roots => 
        Loots.Where(x => x.LootSourceType != LootSourceType.Reference || (uint)x.LootEntry == solutionItem.Entry)
            .Select(x => x.LootEntry)
            .ToList();
    
    public async Task<IQuery> GenerateQuery()
    {
        if (!VerifyDuplicateKeys())
            throw new LootDuplicateKeysException("Some loot items are duplicated, which is illegal. The duplicate rows have been marked red and selected. Please fix it and save again.");
        
        var transaction = Queries.BeginTransaction();
        var updateQuery = queryGenerator.GenerateUpdateLootIds(solutionItem.Type, solutionItem.Entry, solutionItem.Difficulty, Roots);
        transaction.Add(updateQuery);
        
        foreach (var group in Loots)
        {
            var query = queryGenerator.GenerateQuery(new List<LootGroupModel>()
            {
                new(group.LootSourceType, group.LootEntry, group.GroupName, group.DontLoadRecursively, 
                    group.LootItems.Select(loot => loot.ToModel()).ToArray())
            });
            transaction.Add(query);
        }
        
        return transaction.Close();
    }

    private bool VerifyDuplicateKeys()
    {
        List<LootItemViewModel> duplicates = new List<LootItemViewModel>();
        HashSet<(long item, long minPatch)> keys = new();
        for (var groupIndex = 0; groupIndex < Loots.Count; groupIndex++)
        {
            var lootGroup = Loots[groupIndex];
            keys.Clear();
            for (var lootIndex = 0; lootIndex < lootGroup.LootItems.Count; lootIndex++)
            {
                var loot = lootGroup.LootItems[lootIndex];
                loot.IsDuplicate = false;
                var item = loot.ItemOrCurrencyId.Value;
                var minPatch = loot.MinPatch.Value;
                if (!keys.Add((item, minPatch)))
                {
                    if (duplicates.Count == 0)
                        MultiSelection.Clear();

                    duplicates.Add(loot);
                    MultiSelection.Add(new VerticalCursor(groupIndex, lootIndex));
                    loot.IsDuplicate = true;
                }
            }
        }

        return duplicates.Count == 0;
    }

    ISolutionItem ISolutionItemDocument.SolutionItem => SolutionItem;

    public void SortElements()
    {
        using var bulk = HistoryHandler.WithinBulk("Sort loot");
        foreach (var group in Loots)
        {
            var copy = group.LootItems.OrderBy(x => x.GroupId.Value)
                .ThenBy(x => x.ItemOrCurrencyId.Value)
                .ToList();
            group.LootItems.RemoveAll();
            group.LootItems.AddRange(copy);
        }
    }
}