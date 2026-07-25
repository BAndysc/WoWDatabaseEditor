using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Prism.Events;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Utils;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawns.Models;

[UniqueProvider]
public interface IGamePhaseService
{
    ObservableCollection<GamePhaseViewModel> Phases { get; }
    ObservableCollection<GamePhaseViewModel> ActivePhases { get; }
    IObservable<IReadOnlyList<GamePhaseViewModel>> ActivePhasesObservable { get; }
    bool IsPhaseActive(SmallReadOnlyList<int>? phaseId, int? phaseGroup);
    bool PhaseMaskOverlaps(uint phaseMask);
}

[AutoRegister]
[SingleInstance]
public class GamePhaseService : IGamePhaseService
{
    private readonly IPhaseStore phaseStore;
    private uint activePhaseMask;
    public ObservableCollection<GamePhaseViewModel> Phases { get; } = new();
    public ObservableCollection<GamePhaseViewModel> ActivePhases { get; } = new();
    public IObservable<IReadOnlyList<GamePhaseViewModel>> ActivePhasesObservable { get; }

    // IsPhaseActive is called every frame from the game thread while Active toggles/DBC loads
    // mutate ActivePhases on the UI thread - the game thread must only read this immutable snapshot
    private volatile GamePhaseViewModel[] activePhasesSnapshot = [];

    public bool IsPhaseActive(SmallReadOnlyList<int>? phaseIds, int? phaseGroup)
    {
        var activePhases = activePhasesSnapshot;

        if (!phaseIds.HasValue && !phaseGroup.HasValue)
            return true;

        if (phaseGroup.HasValue)
        {
            if (phaseGroup == 0)
                return activePhases.Length == 0;

            var phaseGroupEntry = phaseStore.GetPhaseXPhaseGroupById(phaseGroup.Value);

            if (phaseGroupEntry == null)
                return false;

            foreach (var phaseId in phaseGroupEntry.Phases)
                if (activePhases.Any(x => x.Entry == phaseId))
                    return true;

            return false;
        }

        if (phaseIds.HasValue)
        {
            if (phaseIds.Value.Count == 0 || phaseIds.Value is [0])
            {
                return activePhases.Length == 0;
            }
            else
            {
                foreach (var phaseId in phaseIds)
                    if (activePhases.Any(x => x.Entry == phaseId))
                        return true;
                return false;
            }
        }

        return false;
    }

    public bool PhaseMaskOverlaps(uint phaseMask)
    {
        return (phaseMask & activePhaseMask) != 0;
    }

    public GamePhaseService(IDbcStore dbcStore,
        IPhaseStore phaseStore,
        IEventAggregator eventAggregator,
        ICurrentCoreVersion currentCoreVersion)
    {
        this.phaseStore = phaseStore;
        ActivePhasesObservable = FunctionalExtensions.Select(ActivePhases.ToCountChangedObservable(), _ => ActivePhases);

        if (currentCoreVersion.Current.PhasingType == PhasingType.PhaseIds)
        {
            DbcLoaded(dbcStore);
            eventAggregator.GetEvent<DbcLoadedEvent>().Subscribe(DbcLoaded);
        }
        else
        {
            foreach (var phaseMask in Enum.GetValues<InGamePhase>())
                AddPhase(new GamePhaseViewModel((uint)phaseMask, ""));
            Phases[0].Active = true;
        }
    }

    private void DbcLoaded(IDbcStore dbcStore)
    {
        foreach (var phase in dbcStore.PhaseStore)
        {
            AddPhase(new GamePhaseViewModel((uint)phase.Key, phase.Value));
        }
    }

    // binds exactly the added phase - binding all Phases here again would duplicate
    // the Active handlers on every DbcLoaded
    private void AddPhase(GamePhaseViewModel e)
    {
        Phases.Add(e);
        e.ToObservable(x => x.Active)
            .Skip(1)
            .SubscribeAction(@is =>
            {
                if (@is)
                {
                    activePhaseMask |= e.Entry;
                    ActivePhases.Add(e);
                }
                else
                {
                    activePhaseMask &= ~e.Entry;
                    ActivePhases.Remove(e);
                }
                activePhasesSnapshot = ActivePhases.ToArray();
            });
    }
}