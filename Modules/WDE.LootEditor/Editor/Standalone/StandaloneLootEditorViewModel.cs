using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Ioc;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;
using WDE.LootEditor.Solution;
using WDE.LootEditor.Utils;
using WDE.MVVM;

namespace WDE.LootEditor.Editor.Standalone;

public partial class StandaloneLootEditorViewModel : ObservableBase, IDialog, IWindowViewModel, IClosableDialog
{
    private readonly ICreatureEntryOrGuidProviderService creaturePicker;
    private readonly IGameobjectEntryOrGuidProviderService gameobjectPicker;
    private readonly IDatabaseProvider databaseProvider;
    private readonly IParameterPickerService parameterPickerService;
    private readonly IMessageBoxService messageBoxService;
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
        viewModel = null;
        await LoadLootCommand.ExecuteAsync();
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

    private async Task UpdateAvailableDifficulties(LootSourceType lootType, uint solutionEntry)
    {
        if (this.lootType != LootSourceType.Creature)
        {
            Difficulties.Clear();
            Difficulties.Add(legacyDifficulties[0]);
        }
        else
        {
            var template = await databaseProvider.GetCreatureTemplate(solutionEntry);
            var difficulties = await databaseProvider.GetCreatureTemplateDifficulties(solutionEntry);
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
    
    public StandaloneLootEditorViewModel(
        IContainerProvider containerProvider,
        ICreatureEntryOrGuidProviderService creaturePicker,
        IGameobjectEntryOrGuidProviderService gameobjectPicker,
        IDatabaseProvider databaseProvider,
        IParameterPickerService parameterPickerService,
        IMessageBoxService messageBoxService,
        IWindowManager windowManager,
        ITextDocumentService textDocumentService,
        IParameterFactory parameterFactory
        )
    {
        this.creaturePicker = creaturePicker;
        this.gameobjectPicker = gameobjectPicker;
        this.databaseProvider = databaseProvider;
        this.parameterPickerService = parameterPickerService;
        this.messageBoxService = messageBoxService;
        legacyDifficulties[0] = DifficultyViewModel.Legacy(0, "default");
        legacyDifficulties[1] = DifficultyViewModel.Legacy(1, "heroic dung/25 raid");
        legacyDifficulties[2] = DifficultyViewModel.Legacy(2, "10 heroic raid");
        legacyDifficulties[3] = DifficultyViewModel.Legacy(3, "25 heroic raid");
        allDifficulties = parameterFactory.Factory("DifficultyParameter").Items?
            .Select(x => DifficultyViewModel.Modern((uint)x.Key, x.Value.Name)).ToList() ?? new List<DifficultyViewModel>();
        difficulty = legacyDifficulties[0];
        Difficulties.Add(legacyDifficulties[0]);
        Difficulties.AddRange(allDifficulties);
        
        SaveCommand = new AsyncAutoCommand(async () =>
        {
            if (viewModel is not null)
                await viewModel.Save.ExecuteAsync();
        });
        GenerateQueryCommand = new AsyncAutoCommand(async () =>
        {
            if (viewModel is not null)
                windowManager.ShowStandaloneDocument(textDocumentService.CreateDocument("SQL Query", (await viewModel.GenerateQuery()).QueryString, "sql", false), out _);
        });
        LoadLootCommand = new AsyncAutoCommand(async () =>
        {
            if (!await AskToSave())
                return;

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
            
            var solutionItem = new LootSolutionItem(lootType, actualEntryToLoad, difficultyId);
            ViewModel?.Dispose();
            ViewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(LootSolutionItem), solutionItem));
            await ViewModel.BeginLoad();
        });
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
            UpdateAvailableDifficulties(lootType, solutionEntry).ListenErrors();
        });
        On(() => SolutionEntry, solutionEntry =>
        {
            UpdateAvailableDifficulties(lootType, solutionEntry).ListenErrors();
        });
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
    public string Title => "Loot Editor";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
    public ImageUri? Icon { get; set; }

    public override void Dispose()
    {
        base.Dispose();
        viewModel?.Dispose();
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