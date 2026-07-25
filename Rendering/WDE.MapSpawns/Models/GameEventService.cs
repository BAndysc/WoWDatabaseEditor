using System.Collections.ObjectModel;
using System.Reactive.Linq;
using WDE.Common.Database;
using WDE.Common.Modules;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawns.Models;

[UniqueProvider]
public interface IGameEventService
{
    ObservableCollection<GameEventViewModel> GameEvents { get; }
    ObservableCollection<GameEventViewModel> ActiveEvents { get; }
    IObservable<IReadOnlyList<GameEventViewModel>> ActiveEventsObservable { get; }
    bool IsEventActive(uint eventId);
}

[AutoRegister]
[SingleInstance]
public class GameEventService : IGameEventService, IGlobalAsyncInitializer
{
    private readonly IDatabaseProvider databaseProvider;
    public ObservableCollection<GameEventViewModel> GameEvents { get; private set; } = new();
    public ObservableCollection<GameEventViewModel> ActiveEvents { get; private set; } = new();
    public IObservable<IReadOnlyList<GameEventViewModel>> ActiveEventsObservable { get; set; }

    // IsEventActive is called from the game thread while Active toggles mutate ActiveEvents
    // on the UI thread - the game thread must only read this immutable snapshot
    private volatile GameEventViewModel[] activeEventsSnapshot = [];

    public bool IsEventActive(uint eventId)
    {
        foreach (var ev in activeEventsSnapshot)
            if (ev.Entry == eventId)
                return true;
        return false;
    }

    public GameEventService(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
        ActiveEventsObservable = FunctionalExtensions.Select(ActiveEvents.ToCountChangedObservable(), _ => ActiveEvents);
    }

    public async Task Initialize()
    {
        var gameEvents = await databaseProvider.GetGameEventsAsync();
        // bind Active AFTER the events are loaded - the previous ctor-time binding ran over an
        // empty collection, so toggling an event could never populate ActiveEvents
        foreach (var ge in gameEvents)
        {
            var e = new GameEventViewModel(ge);
            GameEvents.Add(e);
            e.ToObservable(x => x.Active)
                .Skip(1)
                .SubscribeAction(@is =>
                {
                    if (@is)
                        ActiveEvents.Add(e);
                    else
                        ActiveEvents.Remove(e);
                    activeEventsSnapshot = ActiveEvents.ToArray();
                });
        }
    }
}