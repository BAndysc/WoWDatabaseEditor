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

    public override string ToString()
    {
        return Name;
    }
}