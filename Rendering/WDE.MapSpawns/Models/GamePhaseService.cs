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
    private uint activePhaseMask;
    public ObservableCollection<GamePhaseViewModel> Phases { get; } = new();
    public ObservableCollection<GamePhaseViewModel> ActivePhases { get; } = new();
    public IObservable<IReadOnlyList<GamePhaseViewModel>> ActivePhasesObservable { get; }

    public bool IsPhaseActive(SmallReadOnlyList<int>? phaseIds, int? phaseGroup)
    {
        if (!phaseIds.HasValue && !phaseGroup.HasValue)
            return true;

        if (phaseIds.HasValue)
        {
            foreach (var phaseId in phaseIds)
                if (ActivePhases.All(x => x.Entry != phaseId))
                    return false;
            return true;
        }

        throw new Exception("Phase group not yet implemented (implement dbc loading first)");
    }

    public bool PhaseMaskOverlaps(uint phaseMask)
    {
        return (phaseMask & activePhaseMask) != 0;
    }

    public GamePhaseService(IDbcStore dbcStore,
        IEventAggregator eventAggregator,
        ICurrentCoreVersion currentCoreVersion)
    {
        ActivePhasesObservable = FunctionalExtensions.Select(ActivePhases.ToCountChangedObservable(), _ => ActivePhases);

        if (currentCoreVersion.Current.PhasingType == PhasingType.PhaseIds)
        {
            DbcLoaded(dbcStore);
            eventAggregator.GetEvent<DbcLoadedEvent>().Subscribe(DbcLoaded);
            BindPhases();
        }
        else
        {
            foreach (var phaseMask in Enum.GetValues<InGamePhase>())
                Phases.Add(new GamePhaseViewModel((uint)phaseMask, ""));
            BindPhases();
            Phases[0].Active = true;
        }
    }
    
    private void DbcLoaded(IDbcStore dbcStore)
    {
        foreach (var phase in dbcStore.PhaseStore)
        {
            Phases.Add(new GamePhaseViewModel((uint)phase.Key, phase.Value));
        }

        BindPhases();
    }

    private void BindPhases()
    {
        foreach (var e in Phases)
        {
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
                });
        }
    }
}