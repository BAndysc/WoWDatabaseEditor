using System.Collections.ObjectModel;
using TheMaths;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models.Formations;

[UniqueProvider]
public interface IFormationEditorService
{
    /// <summary>All formation links resolvable on the current map (both endpoints loaded). The world
    /// draws all of these; the editor list shows only <see cref="CollectListedFormations"/>.</summary>
    ObservableCollection<EditableFormation> LoadedFormations { get; }

    /// <summary>Fills <paramref name="output"/> with the formations to show in the editor list: the
    /// selected spawn's group plus any group with unsaved changes (self-rows excluded).</summary>
    void CollectListedFormations(List<EditableFormation> output);

    /// <summary>The link the details/delete actions target.</summary>
    EditableFormation? Selected { get; set; }

    /// <summary>When false, links render as faint, non-interactive arrows and dragging is disabled.</summary>
    bool ToolEnabled { get; set; }

    /// <summary>True when the active core has a creature_formations SQL provider (gates the tool).</summary>
    bool IsSupported { get; }

    /// <summary>The map whose formations are currently loaded (-1 if none).</summary>
    int LoadedMap { get; }

    // in-progress drag rubber-band: set by the module, drawn by the render stage
    bool DragActive { get; set; }
    Vector3 DragFrom { get; set; }
    Vector3 DragTo { get; set; }
    bool DragSnapped { get; set; }

    /// <summary>Merges off-thread loaded formations into the collection. Call once per frame on the engine thread.</summary>
    void PumpPendingLoads();

    /// <summary>Keeps formation data and spawn placement in sync as objects are dragged: moving a member
    /// updates its dist/angle; moving the leader drags its members. Call once per frame.</summary>
    void SyncConstraints();

    /// <summary>Loads every creature_formations row whose leader AND member spawn live on this map.</summary>
    Task LoadForMap(int mapId);

    /// <summary>Resolves the leader/member spawn world positions (false if either is missing).</summary>
    bool TryGetEndpoints(EditableFormation f, out Vector3 leaderPos, out Vector3 memberPos);

    /// <summary>Resolves a loaded creature spawn position by guid.</summary>
    bool TryGetCreaturePosition(uint guid, out Vector3 pos);

    /// <summary>Fills <paramref name="output"/> with the member creature spawns of the given leader's
    /// formation (self-rows excluded; empty when the guid leads nothing). Used by the spawn dragger:
    /// moving/rotating a leader drags its members along (<see cref="SyncConstraints"/>), so their
    /// induced transforms must be persisted alongside the leader's.</summary>
    void CollectMemberCreatures(uint leaderGuid, List<CreatureSpawnInstance> output);

    /// <summary>Creates a member→leader link, auto-filling dist/angle from the two spawns. Replaces any
    /// existing link for the same member. Null if either guid isn't a loaded creature.</summary>
    EditableFormation? Add(uint memberGuid, uint leaderGuid);

    void Remove(EditableFormation f);

    bool AnyDirty { get; }

    /// <summary>The exact SQL <see cref="Save"/> would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    WDE.SqlQueryGenerator.IQuery? BuildSaveQuery();

    /// <summary>Saves all dirty leader groups (DELETE-by-leader + bulk INSERT) on the main thread.</summary>
    Task Save();
}
