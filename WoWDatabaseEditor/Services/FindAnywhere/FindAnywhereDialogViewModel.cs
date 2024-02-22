using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.FindAnywhere;

[AutoRegister]
public partial class FindAnywhereDialogViewModel : ObservableBase, IFindAnywhereDialogViewModel
{
    private readonly IDatabaseProvider databaseProvider;
    public int DesiredWidth => 400;
    public int DesiredHeight => 320;
    public string Title => "Find anywhere";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    public AsyncAutoCommand FindCommand { get; }
    public ICommand PickValue { get; }
    [Notify] private string searchText = "";
    [Notify] private FindSourceDialog selectedSource;
    public ObservableCollection<FindSourceDialog> Sources { get; } = new();

    public FindAnywhereDialogViewModel(IParameterPickerService pickerService,
        IParameterFactory parameterFactory,
        IDatabaseProvider databaseProvider,
        IFindAnywhereService findAnywhereService)
    {
        this.databaseProvider = databaseProvider;
        Sources.Add(new FindSourceDialog("Spell", "SpellParameter", "SpellAreaSpellParameter", "SpellOrRankedSpellParameter", "MultiSpellParameter"));
        Sources.Add(new FindSourceDialog("Quest", "QuestParameter"));
        Sources.Add(new FindSourceDialog("Creature entry", "CreatureParameter", "CreatureGameobjectParameter"));
        Sources.Add(new FindSourceDialog("Creature spawn by guid", "CreatureGUIDParameter"));
        Sources.Add(new FindSourceDialogSpawnByEntry("Creature spawn by entry", "CreatureParameter", "CreatureGUIDParameter"));
        Sources.Add(new FindSourceDialog("Game object entry", "GameobjectParameter", "CreatureGameobjectParameter"));
        Sources.Add(new FindSourceDialog("Game object spawn by guid", "GameobjectGUIDParameter"));
        Sources.Add(new FindSourceDialogSpawnByEntry("Game object spawn by entry", "GameobjectParameter", "GameobjectGUIDParameter"));
        Sources.Add(new FindSourceDialog("Faction", "FactionTemplateParameter"));
        Sources.Add(new FindSourceDialog("Map", "MapParameter"));
        Sources.Add(new FindSourceDialog("Zone/area", "ZoneAreaParameter"));
        Sources.Add(new FindSourceDialog("Emote", "EmoteParameter"));
        Sources.Add(new FindSourceDialog("Item", "ItemParameter"));
        Sources.Add(new FindSourceDialog("Sound", "SoundParameter"));
        Sources.Add(new FindSourceDialog("Achievement", "AchievementParameter"));
        Sources.Add(new FindSourceDialog("Skill", "SkillParameter"));
        Sources.Add(new FindSourceDialog("Event", "EventScriptParameter"));
        Sources.Add(new FindSourceDialog("Timed action list", "TimedActionListParameter"));
        Sources.Add(new FindSourceDialog("Trigger Map Event", "MapEventParameter"));
        Sources.Add(new FindSourceDialog("Scenario Event", "ScenarioEventParameter"));
        Sources.Add(new FindSourceDialog("Conversation", "ConversationParameter"));
        
        selectedSource = Sources[0];
        
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        PickValue = new AsyncAutoCommand(async () =>
        {
            var parameter = parameterFactory.Factory(selectedSource.Parameters[0]);
            long searchValue = 0;
            long.TryParse(searchText, out searchValue);
            var result = await pickerService.PickParameter(parameter, searchValue);
            if (result.ok)
                SearchText = result.value.ToString();
        });
        FindCommand = new AsyncAutoCommand(async () =>
        {
            long searchValue = 0;
            long.TryParse(searchText, out searchValue);
            if (selectedSource is FindSourceDialogSpawnByEntry spawnByEntry)
            {
                List<long> guids;
                if (spawnByEntry.EntryParameter == "CreatureParameter")
                {
                    var creatures = await databaseProvider.GetCreaturesByEntryAsync((uint)searchValue);
                    guids = creatures.Select(c => (long)c.Guid).ToList();
                }
                else
                {
                    Debug.Assert(spawnByEntry.EntryParameter == "GameobjectParameter");
                    var gameobjects = await databaseProvider.GetGameObjectsByEntryAsync((uint)searchValue);
                    guids = gameobjects.Select(c => (long)c.Guid).ToList();
                }
                findAnywhereService.OpenFind(new List<string>(){spawnByEntry.SpawnParameter}, guids);
            }
            else
                findAnywhereService.OpenFind(selectedSource.Parameters, searchValue);

            CloseOk?.Invoke();
        }, () => long.TryParse(searchText, out var val) && val != 0);
        
        On(() => SearchText, _ => FindCommand.RaiseCanExecuteChanged());
        
        Accept = new DelegateCommand(() =>
        {
            if (FindCommand.CanExecute(null))
                FindCommand?.Execute(null); 
        });
    }
}