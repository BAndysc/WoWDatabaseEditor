using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Ioc;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Avalonia.Components;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;
using WDE.LootEditor.Solution;
using WDE.LootEditor.Solution.PerDatabaseTable;
using WDE.LootEditor.Solution.PerEntity;
using WDE.LootEditor.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.MVVM.Utils;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.Editor.Standalone;

public partial class StandaloneLootEditorViewModel : ObservableBase, IDialog, IWindowViewModel, IClosableDialog, ISolutionItemDocument
{
    private readonly IContainerProvider containerProvider;
    private readonly ICreatureEntryOrGuidProviderService creaturePicker;
    private readonly IGameobjectEntryOrGuidProviderService gameobjectPicker;
    private readonly IDatabaseProvider databaseProvider;
    private readonly IParameterPickerService parameterPickerService;
    private readonly IMessageBoxService messageBoxService;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private LootSourceType lootType;
    public LootSourceType LootType
    {
        get => lootType;
        set
        {
            var old = lootType;
            if (old == value)
                return;
            lootType = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CanChooseDifficulty));
            TryChange(old, difficulty, solutionEntry).ListenErrors();
        }
    }

    [Notify] private bool canChangeLootType = true;
    [Notify] private bool canChangeEntry = true;
    [Notify] private uint solutionEntry;
    [Notify] private LootEditorViewModel? viewModel;
    private DifficultyViewModel? difficulty;
    public DifficultyViewModel? Difficulty
    {
        get => difficulty;
        set
        {
            var old = difficulty;
            if (old == value)
                return;
            difficulty = value;
            RaisePropertyChanged();
            TryChange(lootType, old, solutionEntry).ListenErrors();
        }
    }

    private async Task TryChange(LootSourceType oldSourceType, DifficultyViewModel? oldDifficulty, uint oldSolutionEntry)
    {
        if (!await AskToSave())
        {
            lootType = oldSourceType;
            difficulty = oldDifficulty;
            solutionEntry = oldSolutionEntry;
            RaisePropertyChanged(nameof(LootType));
            RaisePropertyChanged(nameof(Difficulty));
            RaisePropertyChanged(nameof(SolutionEntry));
            RaisePropertyChanged(nameof(CanChooseDifficulty));
            return;
        }
        viewModel?.Dispose();
        if (IsPerEntityLootEditingMode)
        {
            ViewModel = null;
            await LoadLootCommand.ExecuteAsync();
        }
        else
        {
            ViewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(PerDatabaseTableLootSolutionItem), new PerDatabaseTableLootSolutionItem(lootType)), ((typeof(PerEntityLootSolutionItem), null)));
            await ViewModel.BeginLoad();
        }
    }

    private List<DifficultyViewModel> allDifficulties = new();
    public IReadOnlyCollection<DifficultyViewModel> AllDifficulties => allDifficulties;
    public ObservableCollection<DifficultyViewModel> Difficulties { get; } = new ();

    private DifficultyViewModel[] legacyDifficulties = new DifficultyViewModel[4];
    
    public bool CanChooseDifficulty => lootType == LootSourceType.Creature;
    
    public IAsyncCommand PickEntryCommand { get; }
    public IAsyncCommand LoadLootCommand { get; }
    public IAsyncCommand SaveCommand { get; }
    public IAsyncCommand GenerateQueryCommand { get; }
    public LootEditingMode EditingMode => currentCoreVersion.Current.LootEditingMode;
    public bool IsPerEntityLootEditingMode => EditingMode == LootEditingMode.PerLogicalEntity;
    public bool IsPerDatabaseTableLootEditingMode => EditingMode == LootEditingMode.PerDatabaseTable;

    private async Task UpdateAvailableDifficulties(LootSourceType lootType, uint solutionEntry, CancellationToken token)
    {
        if (this.lootType != LootSourceType.Creature)
        {
            Difficulties.Clear();
            Difficulties.Add(legacyDifficulties[0]);
        }
        else
        {
            var template = await databaseProvider.GetCreatureTemplate(solutionEntry);
            if (token.IsCancellationRequested)
                return;
            
            var difficulties = await databaseProvider.GetCreatureTemplateDifficulties(solutionEntry);
            if (token.IsCancellationRequested)
                return;

            Difficulties.Clear();
            
            Difficulties.Add(legacyDifficulties[0]);
            if (template != null)
            {
                if (template.DifficultyEntry1 != 0)
                    Difficulties.Add(legacyDifficulties[1]);
                if (template.DifficultyEntry2 != 0)
                    Difficulties.Add(legacyDifficulties[2]);
                if (template.DifficultyEntry3 != 0)
                    Difficulties.Add(legacyDifficulties[3]);
            }
            
            foreach (var diff in allDifficulties)
            {
                if (difficulties.Any(x => x.DifficultyId == diff.Id && !diff.IsLegacy))
                    Difficulties.Add(diff);
            }
        }

        if (difficulty != null && !Difficulties.Contains(difficulty))
        {
            Difficulty = Difficulties.FirstOrDefault();
        }
    }

    public async Task<(uint actualEntry, uint actualDifficulty)> GetActualEntryAndDifficulty()
    {
        var actualEntryToLoad = solutionEntry;
        var difficultyId = difficulty?.Id ?? 0;
        if (difficulty != null && difficulty.IsLegacy && difficulty.Id > 0)
        {
            var template = await databaseProvider.GetCreatureTemplate(solutionEntry);
            if (difficulty.Id == 1)
                actualEntryToLoad = template?.DifficultyEntry1 ?? 0;
            else if (difficulty.Id == 2)
                actualEntryToLoad = template?.DifficultyEntry2 ?? 0;
            else if (difficulty.Id == 3)
                actualEntryToLoad = template?.DifficultyEntry3 ?? 0;
            difficultyId = 0;
        }

        return (actualEntryToLoad, difficultyId);
    }
    
    public StandaloneLootEditorViewModel(
        IContainerProvider containerProvider,
        ICreatureEntryOrGuidProviderService creaturePicker,
        IGameobjectEntryOrGuidProviderService gameobjectPicker,
        IDatabaseProvider databaseProvider,
        IParameterPickerService parameterPickerService,
        IMessageBoxService messageBoxService,
        IWindowManager windowManager,
        ITextDocumentService textDocumentService,
        IParameterFactory parameterFactory,
        ICurrentCoreVersion currentCoreVersion,
        PerDatabaseTableLootSolutionItem? solutionItem = null
        )
    {
        this.containerProvider = containerProvider;
        this.creaturePicker = creaturePicker;
        this.gameobjectPicker = gameobjectPicker;
        this.databaseProvider = databaseProvider;
        this.parameterPickerService = parameterPickerService;
        this.messageBoxService = messageBoxService;
        this.currentCoreVersion = currentCoreVersion;
        legacyDifficulties[0] = DifficultyViewModel.Legacy(0, "default");
        legacyDifficulties[1] = DifficultyViewModel.Legacy(1, "heroic dung/25 raid");
        legacyDifficulties[2] = DifficultyViewModel.Legacy(2, "10 heroic raid");
        legacyDifficulties[3] = DifficultyViewModel.Legacy(3, "25 heroic raid");
        allDifficulties = parameterFactory.Factory("DifficultyParameter").Items?
            .Select(x => DifficultyViewModel.Modern((uint)x.Key, x.Value.Name)).ToList() ?? new List<DifficultyViewModel>();
        difficulty = legacyDifficulties[0];
        Difficulties.Add(legacyDifficulties[0]);
        Difficulties.AddRange(allDifficulties);
        lootType = solutionItem?.Type ?? LootSourceType.Creature;
        Undo = new DelegateCommand(() => viewModel?.Undo.Execute(null), () => viewModel?.Undo.CanExecute(null) ?? false);
        Redo = new DelegateCommand(() => viewModel?.Redo.Execute(null), () => viewModel?.Redo.CanExecute(null) ?? false);
        Copy = new DelegateCommand(() => viewModel?.Copy.Execute(null), () => viewModel?.Copy.CanExecute(null) ?? false);
        Cut = new DelegateCommand(() => viewModel?.Cut.Execute(null), () => viewModel?.Cut.CanExecute(null) ?? false);
        Paste = new DelegateCommand(() => viewModel?.Paste.Execute(null), () => viewModel?.Paste.CanExecute(null) ?? false);
        
        Save = SaveCommand = new AsyncAutoCommand(async () =>
        {
            if (viewModel is not null)
                await viewModel.Save.ExecuteAsync();
        });
        GenerateQueryCommand = new AsyncAutoCommand(async () =>
        {
            if (viewModel is not null)
                windowManager.ShowStandaloneDocument(textDocumentService.CreateDocument("SQL Query", (await viewModel.GenerateQuery()).QueryString, "sql", false), out _);
        });
        if (IsPerEntityLootEditingMode)
        {
            Title = "Loot editor";
            LoadLootCommand = new AsyncAutoCommand(async () =>
            {
                if (!await AskToSave())
                    return;
                
                var (actualEntryToLoad, difficultyId) = await GetActualEntryAndDifficulty();
                var solutionItem = new PerEntityLootSolutionItem(lootType, actualEntryToLoad, difficultyId);
                ViewModel?.Dispose();
                ViewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(PerEntityLootSolutionItem), solutionItem), (typeof(PerDatabaseTableLootSolutionItem), null));
                await ViewModel.BeginLoad();
            });
        }
        else
        {
            Title = $"{lootType} loot editor";
            viewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(PerDatabaseTableLootSolutionItem), solutionItem ?? new PerDatabaseTableLootSolutionItem(lootType)), (typeof(PerEntityLootSolutionItem), null));
            viewModel.BeginLoad().ListenErrors();
            LoadLootCommand = new AsyncAutoCommand(async () =>
            {
                if (viewModel.LootSourceType != lootType)
                {
                    if (!await AskToSave())
                        return;

                    ViewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(PerDatabaseTableLootSolutionItem), solutionItem ?? new PerDatabaseTableLootSolutionItem(lootType)), (typeof(PerEntityLootSolutionItem), null));
                    await ViewModel.BeginLoad();
                }

                var (actualEntryToLoad, difficultyId) = await GetActualEntryAndDifficulty();
                await viewModel!.AddLootFromEntity(lootType, actualEntryToLoad, difficultyId);
            }).WrapMessageBox<Exception>(messageBoxService);
        }
        PickEntryCommand = new AsyncAutoCommand(async () =>
        {
            var (newEntry, ok) = await parameterPickerService.PickParameter(lootType.GetParameterFor());
            
            if (ok)
            {
                SolutionEntry = (uint)newEntry;
                await LoadLootCommand.ExecuteAsync();
            }
        });
        Accept = new AsyncAutoCommand(async () =>
        {
            if (viewModel != null)
                await viewModel.Save.ExecuteAsync();
            CloseOk?.Invoke();
        });
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        
        On(() => LootType, lootType =>
        {
            difficultyLoadToken?.Cancel();
            difficultyLoadToken = new CancellationTokenSource();
            UpdateAvailableDifficulties(lootType, solutionEntry, difficultyLoadToken.Token).ListenErrors();
        });
        On(() => SolutionEntry, solutionEntry =>
        {
            difficultyLoadToken?.Cancel();
            difficultyLoadToken = new CancellationTokenSource();
            UpdateAvailableDifficulties(lootType, solutionEntry, difficultyLoadToken.Token).ListenErrors();
        });
        On(() => ViewModel, BindViewModelUndoRedo);
    }

    private CancellationTokenSource? difficultyLoadToken;
    private System.IDisposable? boundUndoRedo;
    private LootEditorViewModel? boundCopyCutPaste;
    
    private void BindViewModelUndoRedo(LootEditorViewModel? vm)
    {
        if (boundCopyCutPaste != null)
        {
            boundCopyCutPaste.Copy.CanExecuteChanged -= RaiseCopyCutPasteChanged;
            boundCopyCutPaste.Cut.CanExecuteChanged -= RaiseCopyCutPasteChanged;
            boundCopyCutPaste.Paste.CanExecuteChanged -= RaiseCopyCutPasteChanged;
            boundCopyCutPaste = null;
        }
        boundUndoRedo?.Dispose();
        boundUndoRedo = null;

        if (vm != null)
        {
            boundCopyCutPaste = vm;
            boundCopyCutPaste.Copy.CanExecuteChanged += RaiseCopyCutPasteChanged;
            boundCopyCutPaste.Cut.CanExecuteChanged += RaiseCopyCutPasteChanged;
            boundCopyCutPaste.Paste.CanExecuteChanged += RaiseCopyCutPasteChanged;
            boundUndoRedo = new CompositeDisposable(
                vm.History.ToObservable(x => x.CanUndo)
                    .SubscribeAction(x => ((DelegateCommand)Undo).RaiseCanExecuteChanged()),
                vm.History.ToObservable(x => x.CanRedo)
                    .SubscribeAction(x => ((DelegateCommand)Redo).RaiseCanExecuteChanged()),
                vm.ToObservable(x => x.IsModified)
                    .SubscribeAction(x => RaisePropertyChanged(nameof(IsModified))));
        }
    }

    private void RaiseCopyCutPasteChanged(object? sender, EventArgs e)
    {
        ((DelegateCommand)Copy).RaiseCanExecuteChanged();
        ((DelegateCommand)Cut).RaiseCanExecuteChanged();
        ((DelegateCommand)Paste).RaiseCanExecuteChanged();
    }

    private async Task<bool> AskToSave()
    {
        if (viewModel != null && viewModel.IsModified)
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<SaveDialogResult>()
                .SetTitle("Save changes?")
                .SetMainInstruction("Do you want to save changes?")
                .SetContent("The loot is modified. Unsaved changes will be lost.")
                .WithYesButton(SaveDialogResult.Save)
                .WithNoButton(SaveDialogResult.DontSave)
                .WithCancelButton(SaveDialogResult.Cancel)
                .Build());
                
            if (result == SaveDialogResult.Cancel)
                return false;

            if (result == SaveDialogResult.Save)
                await viewModel.Save.ExecuteAsync();
        }

        return true;
    }

    public int DesiredWidth => 900;
    public int DesiredHeight => 800;
    public string Title { get; }
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
    public ImageUri? Icon => new ImageUri("Icons/document_loot.png");
    public ICommand Copy { get; }
    public ICommand Cut { get; }
    public ICommand Paste { get; }
    public IAsyncCommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;

    public override void Dispose()
    {
        base.Dispose();
        viewModel?.Dispose();
        boundUndoRedo?.Dispose();
    }

    public void OnClose()
    {
        async Task AskToSaveAndClose()
        {
            if (!await AskToSave())
                return;
            
            CloseCancel?.Invoke();
        }
        AskToSaveAndClose().ListenErrors();
    }

    public event Action? Close;

    public void SetWithoutLoading(LootSourceType type, uint entry, DifficultyViewModel? difficulty)
    {
        lootType = type;
        solutionEntry = entry;
        this.difficulty = difficulty ?? legacyDifficulties[0];
    }

    public ICommand Undo { get; }
    public ICommand Redo { get; }
    public IHistoryManager? History => viewModel?.History;
    public bool IsModified => viewModel?.IsModified ?? false;
    public ISolutionItem SolutionItem => viewModel?.SolutionItem ?? new PerDatabaseTableLootSolutionItem(lootType);
    public async Task<IQuery> GenerateQuery()
    {
        if (viewModel != null)
            return await viewModel.GenerateQuery();
        return Queries.Empty(DataDatabaseType.World);
    }
}

public class DifficultyViewModel
{
    public uint Id { get; }
    public string Name { get; }
    public bool IsLegacy { get; }
    private readonly string toString;

    private DifficultyViewModel(uint id, string name, bool legacy)
    {
        Id = id;
        Name = name;
        IsLegacy = legacy;
        toString = $"{name} ({id})";
    }

    public override string ToString() => toString;

    public static DifficultyViewModel Legacy(uint id, string name)
    {
        return new DifficultyViewModel(id, name, true);
    }
    
    public static DifficultyViewModel Modern(uint id, string name)
    {
        return new DifficultyViewModel(id, name, false);
    }
}