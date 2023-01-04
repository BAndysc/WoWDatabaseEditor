using System.Collections.ObjectModel;
using System.Reactive.Linq;
using WDE.Common.Database;
using WDE.Common.Modules;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawnsEditor.Models;

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
    
    public bool IsEventActive(uint eventId)
    {
        foreach (var ev in ActiveEvents)
            if (ev.Entry == eventId)
                return true;
        return false;
    }

    public GameEventService(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
        ActiveEventsObservable = FunctionalExtensions.Select(ActiveEvents.ToCountChangedObservable(), _ => ActiveEvents);

        foreach (var e in GameEvents)
        {
            e.ToObservable(x => x.Active)
                .Skip(1)
                .SubscribeAction(@is =>
                {
                    if (@is)
                        ActiveEvents.Add(e);
                    else
                        ActiveEvents.Remove(e);
                });
        }
    }

    public async Task Initialize()
    {
        var gameEvents = await databaseProvider.GetGameEventsAsync();
        GameEvents.AddRange(gameEvents.Select(ge => new GameEventViewModel(ge)));
    }
}
