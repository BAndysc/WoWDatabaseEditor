using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Collections;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Services.RacePickerService;

public partial class RaceProviderViewModel : ObservableBase, IDialog
{
    [Notify] private string searchText = "";
    [Notify] private GameRaceExpansionViewModel selectedExpansion;
    public ObservableCollectionExtended<GameRaceViewModel> FilteredItems { get; } = new();
    public int DesiredWidth => 500;
    public int DesiredHeight => 800;
    public string Title => "Pick races";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    private static readonly CharacterRaces[] AllRacesEnum = new[]
    {
        CharacterRaces.Human,
        CharacterRaces.Orc,
        CharacterRaces.Dwarf,
        CharacterRaces.NightElf,
        CharacterRaces.Undead,
        CharacterRaces.Tauren,
        CharacterRaces.Gnome,
        CharacterRaces.Troll,
        CharacterRaces.Goblin,
        CharacterRaces.BloodElf,
        CharacterRaces.Draenei,
        CharacterRaces.Worgen,
        CharacterRaces.Pandaren,
        CharacterRaces.PandarenAlliance,
        CharacterRaces.PandarenHorde,
        CharacterRaces.Nightborne,
        CharacterRaces.HighmountainTauren,
        CharacterRaces.VoidElf,
        CharacterRaces.LightforgedDraenei,
        CharacterRaces.ZandalariTroll,
        CharacterRaces.KulTiran,
        CharacterRaces.DarkIronDwarf,
        CharacterRaces.Vulpera,
        CharacterRaces.MagharOrc,
        CharacterRaces.Mechagnome,
        CharacterRaces.DracthyrAlliance,
        CharacterRaces.DracthyrHorde,
    };

    private List<GameRaceViewModel> allRaces = new();

    public ObservableCollection<GameRaceExpansionViewModel> Expansions { get; } = new();

    private CharacterRaces value;
    public CharacterRaces Value
    {
        get => value;
        set
        {
            this.value = value;
            allRaces.ForEach(x => x.RaiseIsChecked());
        }
    }

    public RaceProviderViewModel(ICurrentCoreVersion currentCoreVersion,
        CharacterRaces currentValue)
    {
        Expansions.Add(new GameRaceExpansionViewModel(1, "Vanilla", CharacterRaces.AllVanilla));
        Expansions.Add(new GameRaceExpansionViewModel(2, "TBC", CharacterRaces.AllTbc));
        Expansions.Add(new GameRaceExpansionViewModel(3, "WotLK", CharacterRaces.AllWrath));
        Expansions.Add(new GameRaceExpansionViewModel(4, "Cata", CharacterRaces.AllCatataclysm));
        Expansions.Add(new GameRaceExpansionViewModel(5, "MoP", CharacterRaces.AllMoP));
        Expansions.Add(new GameRaceExpansionViewModel(6, "WoD", CharacterRaces.AllWoD));
        Expansions.Add(new GameRaceExpansionViewModel(7, "Legion", CharacterRaces.AllLegion));
        Expansions.Add(new GameRaceExpansionViewModel(8, "BfA", CharacterRaces.AllBfA));
        Expansions.Add(new GameRaceExpansionViewModel(9, "SL", CharacterRaces.AllShadowlands));
        Expansions.Add(new GameRaceExpansionViewModel(10, "Dragonflight", CharacterRaces.AllDragonflight));

        var currentMajorVersion = currentCoreVersion.Current.Version.Major;
        selectedExpansion = Expansions.FirstOrDefault(x => x.MajorVersion == currentMajorVersion) ?? Expansions.First();

        allRaces.Add(new GameRaceViewModel(this, 0, false, null));

        foreach (var expansion in Expansions)
        {
            allRaces.Add(new GameRaceViewModel(this, CharacterRaces.AllHorde & expansion.Mask, false, expansion.MajorVersion, "Horde"));
            allRaces.Add(new GameRaceViewModel(this, CharacterRaces.AllAlliance & expansion.Mask, false, expansion.MajorVersion, "Alliance"));
        }

        foreach (var race in AllRacesEnum)
            allRaces.Add(new GameRaceViewModel(this, race,  true, null));

        Value = currentValue;
        var unsupportedRacesInCurrentExpansion = currentValue & ~selectedExpansion.Mask;
        if (unsupportedRacesInCurrentExpansion != 0)
            selectedExpansion = Expansions.FirstOrDefault(x => x.Mask.HasAllFlagsFast(currentValue)) ?? selectedExpansion;
        OnSelectedExpansionChanged();

        Accept = new DelegateCommand(() => CloseOk?.Invoke());
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
    }

    private void OnSearchTextChanged()
    {
        DoSearch();
    }

    private void OnSelectedExpansionChanged()
    {
        DoSearch();
    }

    private void DoSearch()
    {
        using var _ = FilteredItems.SuspendNotifications();
        FilteredItems.Clear();
        foreach (var race in allRaces)
        {
            if (race.RequiresMajorVersion.HasValue && race.RequiresMajorVersion.Value != selectedExpansion.MajorVersion)
                continue;

            if (race.Race != 0 && !selectedExpansion.Mask.HasFlagFast(race.Race))
                continue;

            if (string.IsNullOrEmpty(searchText) || race.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(race);
            }
        }
        Value &= selectedExpansion.Mask;
    }
}

public class GameRaceViewModel : ObservableBase
{
    private readonly RaceProviderViewModel vm;
    public CharacterRaces Race { get; }
    public bool DrawIcon { get; }
    public int? RequiresMajorVersion { get; }
    public string Name { get; }

    public bool IsChecked
    {
        get => Race == 0 && vm.Value == 0 || Race != 0 && vm.Value != 0 && vm.Value.HasAllFlagsFast(Race);
        set
        {
            if (Race == 0)
            {
                vm.Value = 0;
            }
            else
            {
                if (value)
                    vm.Value |= Race;
                else
                    vm.Value &= ~Race;
            }
        }
    }

    public GameRaceViewModel(RaceProviderViewModel vm, CharacterRaces race, bool drawIcon, int? requiresMajorVersion, string? name = null)
    {
        this.vm = vm;
        this.Race = race;
        DrawIcon = drawIcon;
        RequiresMajorVersion = requiresMajorVersion;
        Name = name ?? Race.ToString().ToTitleCase();
    }

    public void RaiseIsChecked()
    {
        RaisePropertyChanged(nameof(IsChecked));
    }
}

public class GameRaceExpansionViewModel : ObservableBase
{
    public GameRaceExpansionViewModel(int majorVersion, string name, CharacterRaces mask)
    {
        MajorVersion = majorVersion;
        Name = name;
        Mask = mask;
    }

    public int MajorVersion { get; }
    public string Name { get; }
    public CharacterRaces Mask { get; }
}