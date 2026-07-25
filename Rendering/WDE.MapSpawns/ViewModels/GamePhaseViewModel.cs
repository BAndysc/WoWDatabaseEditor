using PropertyChanged.SourceGenerator;
using WDE.MVVM;

namespace WDE.MapSpawns.ViewModels;

public partial class GamePhaseViewModel : ObservableBase
{
    [Notify] private bool active;
    
    public GamePhaseViewModel(uint entry, string name)
    {
        Entry = entry;
        Name = name;
    }

    public uint Entry { get; }
    public string Name { get; }

    private string? displayLabel;
    /// <summary>Cached: mask phases have no DBC name, id phases show "id name".</summary>
    public string DisplayLabel => displayLabel ??=
        string.IsNullOrEmpty(Name) ? $"Phase {Entry}" : $"{Entry}  {Name}";

    public override string ToString()
    {
        return Name;
    }
}