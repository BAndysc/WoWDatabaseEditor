using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Prism.Events;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawnsEditor.Models;

[UniqueProvider]
public interface IGamePhaseService
{
    ObservableCollection<GamePhaseViewModel> Phases { get; }
    System.IObservable<Unit> ActivePhasesObservable { get; }
    bool ShowAllPhases { get; set; }
    bool IsVisible(uint? phaseMask, SmallReadOnlyList<int>? phaseId, int? phaseGroup);
}

[AutoRegister]
[SingleInstance]
public class GamePhaseService : IGamePhaseService
{
    private readonly IParameterFactory parameterFactory;
    private Subject<Unit> activePhasesObservable = new();
    private HashSet<int> activePhases { get; } = new();
    private uint activePhaseMask;
    private bool showAllPhases;
    public ObservableCollection<GamePhaseViewModel> Phases { get; } = new();
    public System.IObservable<Unit> ActivePhasesObservable => activePhasesObservable;
    public PhasingType PhasingType { get; }

    public bool ShowAllPhases
    {
        get => showAllPhases;
        set
        {
            showAllPhases = value;
            activePhasesObservable.OnNext(default);
        }
    }

    public GamePhaseService(IParameterFactory parameterFactory,
        IEventAggregator eventAggregator,
        ICurrentCoreVersion currentCoreVersion)
    {
        this.parameterFactory = parameterFactory;
        PhasingType = currentCoreVersion.Current.PhasingType;

        if (PhasingType is PhasingType.PhaseIds or PhasingType.Both)
        {
            this.parameterFactory.OnRegister("PhaseParameter").SubscribeAction(phases =>
            {
                if (phases is IDynamicParameter<long> dynP)
                {
                    dynP.ItemsChanged += OnPhasesChanged;
                    OnPhasesChanged(dynP);
                }
            });
        }
        
        if (PhasingType is PhasingType.PhaseMasks or PhasingType.Both)
        {
            foreach (var phaseMask in Enum.GetValues<InGamePhase>())
            {
                var vm = AddViewModel(GamePhaseViewModel.FromPhaseMask((uint)phaseMask));
                if (phaseMask == InGamePhase.Phase1)
                    vm.Active = true;
            }
        }
    }

    public bool IsVisible(uint? phaseMask, SmallReadOnlyList<int>? phaseIds, int? phaseGroup)
    {
        if (showAllPhases)
            return true;
        
        if (PhasingType is PhasingType.PhaseIds)
        {
            if (phaseIds == null || phaseIds.Value.Count == 0)
                return true;
            
            foreach (var phaseId in phaseIds)
                if (!activePhases.Contains(phaseId))
                    return false;
            return true;
        }
        else if (PhasingType is PhasingType.PhaseMasks)
        {
            return (phaseMask & activePhaseMask) != 0;
        }
        else if (PhasingType is PhasingType.Both)
        {
            if (phaseIds != null && phaseIds.Value.Count > 0)
            {
                foreach (var phaseId in phaseIds)
                    if (!activePhases.Contains(phaseId))
                        return false;
                return true;
            }
            
            return (phaseMask & activePhaseMask) != 0;
        }
        else if (PhasingType is PhasingType.NoPhasing)
            return true;
        else
            throw new ArgumentOutOfRangeException(nameof(PhasingType), PhasingType, "Phasing type not implemented");
    }

    private void OnPhasesChanged(IParameter<long> phases)
    {
        if (phases.Items != null)
        {
            foreach (var phase in phases.Items)
            {
                if (Phases.FirstOrDefault(x => x.Entry == (uint)phase.Key && x.IsPhaseId) is { } existing)
                    existing.Name = phase.Value.Name;
                else
                    AddViewModel(GamePhaseViewModel.FromPhaseId((uint)phase.Key, phase.Value.Name));
            }
        }
    }

    private GamePhaseViewModel AddViewModel(GamePhaseViewModel vm)
    {
        if (vm.IsPhaseMask)
        {
            vm.ToObservable(x => x.Active)
                .Skip(1)
                .SubscribeAction(@is =>
                {
                    if (@is)
                    {
                        activePhaseMask |= vm.Entry;
                    }
                    else
                    {
                        activePhaseMask &= ~vm.Entry;
                    }
                    activePhasesObservable.OnNext(default);
                });
        }
        else
        {
            vm.ToObservable(x => x.Active)
                .Skip(1)
                .SubscribeAction(@is =>
                {
                    if (@is)
                    {
                        activePhases.Add((int)vm.Entry);
                    }
                    else
                    {
                        activePhases.Remove((int)vm.Entry);
                    }
                    activePhasesObservable.OnNext(default);
                });
        }
        
        Phases.Add(vm);
        return vm;
    }
}