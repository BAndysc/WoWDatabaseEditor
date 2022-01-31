using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using AvaloniaGraph.Controls;
using AvaloniaGraph.GraphLayout;
using AvaloniaGraph.ViewModels;
using JetBrains.Annotations;
using WDE.Common.Avalonia.Components;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.ViewModels;

[UniqueProvider]
public interface IMiniIcons
{
    IEnumerable<RaceMiniIconViewModel> SupportedRaces { get; }
    IEnumerable<ClassMiniIconViewModel> SupportedClasses { get; }
    bool IsAllClasses(CharacterClasses classes);
    bool IsAllRaces(CharacterRaces races);
    bool IsHorde(CharacterRaces races);
    bool IsAlliance(CharacterRaces races);
    IEnumerable<MiniIconViewModel> GetRaceIcons(CharacterRaces races);
}

[SingleInstance]
[AutoRegister]
public class MiniIcons : IMiniIcons
{
    private readonly ICurrentCoreVersion coreVersion;
    private static RaceMiniIconViewModel Human { get; } = new(CharacterRaces.Human, "human", "Human");
    private static RaceMiniIconViewModel Orc { get; } = new(CharacterRaces.Orc, "orc", "Orc");
    private static RaceMiniIconViewModel Dwarf { get; } = new(CharacterRaces.Dwarf, "dwarf", "Dwarf");
    private static RaceMiniIconViewModel NightElf { get; } = new(CharacterRaces.NightElf, "night_elf", "Night Elf");
    private static RaceMiniIconViewModel Undead { get; } = new(CharacterRaces.Undead, "undead", "Undead");
    private static RaceMiniIconViewModel Tauren { get; } = new(CharacterRaces.Tauren, "tauren", "Tauren");
    private static RaceMiniIconViewModel Gnome { get; } = new(CharacterRaces.Gnome, "gnome", "Gnome");
    private static RaceMiniIconViewModel Troll { get; } = new(CharacterRaces.Troll, "troll", "Troll");
    private static RaceMiniIconViewModel Goblin { get; } = new(CharacterRaces.Goblin, "goblin", "Goblin");
    private static RaceMiniIconViewModel BloodElf { get; } = new(CharacterRaces.BloodElf, "blood_elf", "Blood Elf");
    private static RaceMiniIconViewModel Draenei { get; } = new(CharacterRaces.Draenei, "draenei", "Draenei");
    private static RaceMiniIconViewModel Worgen { get; } = new(CharacterRaces.Worgen, "worgen", "Worgen");
    private static RaceMiniIconViewModel Pandaren { get; } = new(CharacterRaces.Pandaren, "pandaren", "Pandaren");
    private static RaceMiniIconViewModel PandarenHorde { get; } = new(CharacterRaces.PandarenHorde, "pandaren", "Pandaren (H)");
    private static RaceMiniIconViewModel PandarenAlliance { get; } = new(CharacterRaces.PandarenAlliance, "pandaren", "Pandaren (A)");
    
    private static ClassMiniIconViewModel DeathKnight { get; } = new(CharacterClasses.DeathKnight, "death_knight", "Death Knight");
    private static ClassMiniIconViewModel Druid { get; } = new(CharacterClasses.Druid, "druid", "Druid");
    private static ClassMiniIconViewModel Hunter { get; } = new(CharacterClasses.Hunter, "hunter", "Hunter");
    private static ClassMiniIconViewModel Mage { get; } = new(CharacterClasses.Mage, "mage", "Mage");
    private static ClassMiniIconViewModel Monk { get; } = new(CharacterClasses.Monk, "monk", "Monk");
    private static ClassMiniIconViewModel Paladin { get; } = new(CharacterClasses.Paladin, "paladin", "Paladin");
    private static ClassMiniIconViewModel Priest { get; } = new(CharacterClasses.Priest, "priest", "Priest");
    private static ClassMiniIconViewModel Rogue { get; } = new(CharacterClasses.Rogue, "rogue", "Rogue");
    private static ClassMiniIconViewModel Shaman { get; } = new(CharacterClasses.Shaman, "shaman", "Shaman");
    private static ClassMiniIconViewModel Warlock { get; } = new(CharacterClasses.Warlock, "warlock", "Warlock");
    private static ClassMiniIconViewModel Warrior { get; } = new(CharacterClasses.Warrior, "warrior", "Warrior");
    private static ClassMiniIconViewModel DemonHunter { get; } = new(CharacterClasses.DemonHunter, "demon_hunter", "Demon Hunter");

    private static MiniIconViewModel Horde { get; } = new MiniIconViewModel(new ImageUri("Icons/icon_horde.png"), "Horde");
    private static MiniIconViewModel Alliance { get; } = new MiniIconViewModel(new ImageUri("Icons/icon_alliance.png"), "Alliance");
    
    private static IReadOnlyList<ClassMiniIconViewModel> AllClasses { get; } = new[]
    {
        Warrior,
        Paladin,
        Hunter,
        Rogue,
        Priest,
        DeathKnight,
        Shaman,
        Mage,
        Warlock,
        Monk,
        Druid,
        DemonHunter
    };

    private static IReadOnlyList<RaceMiniIconViewModel> AllRaces { get; } = new RaceMiniIconViewModel[]
    {
        Human,
        Orc,
        Dwarf,
        NightElf,
        Undead,
        Tauren,
        Gnome,
        Troll,
        Goblin,
        BloodElf,
        Draenei,
        Worgen,
        Pandaren
    };

    public MiniIcons(ICurrentCoreVersion coreVersion)
    {
        this.coreVersion = coreVersion;
        SupportedClasses = AllClasses
            .Where(@class => coreVersion.Current.GameVersionFeatures.AllClasses.HasFlag(@class.Class)).ToList();
        SupportedRaces = AllRaces
            .Where(race => coreVersion.Current.GameVersionFeatures.AllRaces.HasFlag(race.Race)).ToList();
    }

    public IEnumerable<RaceMiniIconViewModel> SupportedRaces { get; }
    public IEnumerable<ClassMiniIconViewModel> SupportedClasses { get; }
    
    public bool IsAllClasses(CharacterClasses classes)
    {
        return coreVersion.Current.GameVersionFeatures.AllClasses == classes;
    }

    public bool IsAllRaces(CharacterRaces races)
    {
        return coreVersion.Current.GameVersionFeatures.AllRaces == races;
    }

    public bool IsHorde(CharacterRaces races)
    {
        var horde = coreVersion.Current.GameVersionFeatures.AllRaces & CharacterRaces.AllHorde;
        return horde == races;
    }

    public bool IsAlliance(CharacterRaces races)
    {
        var alliance = coreVersion.Current.GameVersionFeatures.AllRaces & CharacterRaces.AllAlliance;
        return alliance == races;
    }

    public IEnumerable<MiniIconViewModel> GetRaceIcons(CharacterRaces races)
    {
        if (races == CharacterRaces.None || 
            IsAllRaces(races))
            yield break;

        if (IsHorde(races))
            yield return Horde;
        else if (IsAlliance(races))
            yield return Alliance;
        else
        {
            foreach (var classMiniIconViewModel in SupportedRaces)
            {
                if (races.HasFlag(classMiniIconViewModel.Race))
                    yield return classMiniIconViewModel;
            }   
        }
    }
}

public class MiniIconViewModel : INotifyPropertyChanged
{
    public ImageUri Icon { get; }
    public string Name { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public MiniIconViewModel(ImageUri icon, string name)
    {
        Icon = icon;
        Name = name;
    }
    
    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RaceMiniIconViewModel : MiniIconViewModel
{
    public RaceMiniIconViewModel(CharacterRaces race, string icon, string name) : base(new ImageUri("Icons/races/" + icon + ".png"), name)
    {
        Race = @race;
    }

    public CharacterRaces Race { get; }
}

public class ClassMiniIconViewModel : MiniIconViewModel
{
    public ClassMiniIconViewModel(CharacterClasses @class, string icon, string name) : base(new ImageUri("Icons/classes/" + icon + ".png"), name)
    {
        Class = @class;
    }

    public CharacterClasses Class { get; }
}

public class QuestViewModel : NodeViewModelBase<QuestViewModel>
{
    //public uint Entry { get; }
    public string Name { get; }
    
    public QuestViewModel(IQuestTemplate template, IMiniIcons miniIcons)
    {
        InputConnector = AddInputConnector(ConnectorAttachMode.Top, "", Colors.Aqua);
        OutputConnector = AddOutputConnector(ConnectorAttachMode.Bottom, "", Colors.Aqua);
        LeftInputConnector = AddInputConnector(ConnectorAttachMode.Left, "", Colors.Red);
        RightOutputConnector = AddOutputConnector(ConnectorAttachMode.Right, "", Colors.Red);
        Entry = template.Entry;
        Name = template.Name;

        if (template.AllowableClasses != CharacterClasses.None &&
            !miniIcons.IsAllClasses(template.AllowableClasses))
        {
            Classes = new();
            foreach (var classMiniIconViewModel in miniIcons.SupportedClasses)
            {
                if (template.AllowableClasses.HasFlag(classMiniIconViewModel.Class))
                    Classes.Add(classMiniIconViewModel);
            }
        }

        foreach (var icon in miniIcons.GetRaceIcons(template.AllowableRaces))
        {
            Races ??= new();
            Races.Add(icon);
        }
    }

    public List<MiniIconViewModel>? Classes { get; }
    public List<MiniIconViewModel>? Races { get; }
    
    public override TreeNodeIterator ChildrenIterator => new TreeNodeIterator(OutputConnector.Connections);

    public InputConnectorViewModel<QuestViewModel> InputConnector { get; }
    public OutputConnectorViewModel<QuestViewModel> OutputConnector { get; }

    public InputConnectorViewModel<QuestViewModel> LeftInputConnector { get; }
    public OutputConnectorViewModel<QuestViewModel> RightOutputConnector { get; }
}