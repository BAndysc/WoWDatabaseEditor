using PropertyChanged.SourceGenerator;
using WDE.MVVM;

namespace WDE.MapSpawnsEditor.ViewModels;

public partial class GamePhaseViewModel : ObservableBase
{
    [Notify] private bool active;
    [Notify] private string name = "";
    private bool isPhaseMask;
    
    private GamePhaseViewModel(uint entry, string name, bool isPhaseMask)
    {
        Entry = entry;
        this.name = name;
        this.isPhaseMask = isPhaseMask;
    }

    public static GamePhaseViewModel FromPhaseId(uint entry, string name)
    {
        return new GamePhaseViewModel(entry, name, false);
    }

    public static GamePhaseViewModel FromPhaseMask(uint mask)
    {
        return new GamePhaseViewModel(mask, "", true);
    }

    public uint Entry { get; }
    public bool IsPhaseMask => isPhaseMask;
    public bool IsPhaseId => !isPhaseMask;

    public override string ToString()
    {
        return name;
    }
}