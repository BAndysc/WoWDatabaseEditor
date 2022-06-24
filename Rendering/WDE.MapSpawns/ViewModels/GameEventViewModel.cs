using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.MVVM;

namespace WDE.MapSpawns.ViewModels;

public partial class GameEventViewModel : ObservableBase
{
    [Notify] private bool active;
    public ushort Entry { get; }
    public string Name { get; }

    public GameEventViewModel(IGameEvent ev)
    {
        Entry = ev.Entry;
        Name = ev.Description ?? "Event " + Entry;
    }
}