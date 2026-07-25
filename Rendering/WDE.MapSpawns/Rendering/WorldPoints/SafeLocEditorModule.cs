using TheEngine;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.WorldPoints;

namespace WDE.MapSpawns.Rendering.WorldPoints;

/// <summary>
/// The Graveyards tool: every world_safe_locs row on the current map renders as a beam marker
/// (tinted by which team can use it on death), the gizmo moves/rotates the selected one, and the
/// inspector (<see cref="SafeLocInspector"/>) edits the name and the game_graveyard_zone links.
/// </summary>
public class SafeLocEditorModule : WorldPointModuleBase
{
    private static readonly Vector4 BothColor = new(1.00f, 0.75f, 0.15f, 0.95f);      // gold
    private static readonly Vector4 AllianceColor = new(0.25f, 0.55f, 1.00f, 0.95f);  // blue
    private static readonly Vector4 HordeColor = new(1.00f, 0.30f, 0.25f, 0.95f);     // red
    private static readonly Vector4 UnlinkedColor = new(0.65f, 0.65f, 0.65f, 0.9f);   // gray

    private const uint FactionHorde = 67;
    private const uint FactionAlliance = 469;

    private readonly ISafeLocEditorService service;
    private readonly SafeLocInspector inspector;
    private bool loadInFlight;

    public ISafeLocEditorService Service => service;

    public SafeLocEditorModule(Engine engine,
        IGameContext gameContext,
        ISpawnEditorToolService toolService,
        IWorldInteractionService interaction,
        IInputManager inputManager,
        IGameViewOverlayService overlays,
        RaycastSystem raycastSystem,
        ISafeLocEditorService service)
        : base(engine, gameContext, toolService, interaction, inputManager, overlays, raycastSystem)
    {
        this.service = service;
        inspector = new SafeLocInspector(service, this, gameContext);
    }

    protected override SpawnEditorTool Tool => SpawnEditorTool.Graveyard;
    protected override int GizmoId => 2;
    protected override IInspectorSection Section => inspector;

    // the data is GLOBAL (links may point across maps), so it loads once and survives map
    // switches - unsaved edits are not lost by flying to another map
    protected override void PumpAndSync()
    {
        service.PumpPendingLoads();
        if (!service.IsSupported || loadInFlight || service.HasData)
            return;
        loadInFlight = true;
        Load().ListenErrors();
    }

    public void Reload()
    {
        if (loadInFlight)
            return;
        loadInFlight = true;
        Load().ListenErrors();
    }

    private async Task Load()
    {
        try
        {
            await service.LoadForMap(CurrentMapId);
        }
        finally
        {
            loadInFlight = false;
        }
    }

    public static Vector4 ColorOf(SafeLocData loc)
    {
        if (loc.Links.Count == 0)
            return UnlinkedColor;
        bool anyBoth = false, anyAlliance = false, anyHorde = false;
        foreach (var link in loc.Links)
        {
            anyBoth |= link.Faction == 0;
            anyAlliance |= link.Faction == FactionAlliance;
            anyHorde |= link.Faction == FactionHorde;
        }
        if (anyBoth || (anyAlliance && anyHorde))
            return BothColor;
        if (anyAlliance)
            return AllianceColor;
        if (anyHorde)
            return HordeColor;
        return BothColor; // unknown faction value - treat as both
    }

    protected override int DataRevision => service.Revision;

    protected override void CollectPoints(List<WorldPoint> output)
    {
        var map = (uint)CurrentMapId;
        foreach (var loc in service.Locs.Values)
        {
            if (loc.Map != map)
                continue;
            output.Add(new WorldPoint
            {
                Key = loc.Id,
                Position = loc.Position,
                Orientation = loc.Orientation,
                Label = $"{loc.Name} #{loc.Id}",
                Color = ColorOf(loc),
            });
        }
    }

    protected override bool TryGetSelectedTransform(out Vector3 position, out float orientation)
    {
        position = default;
        orientation = 0;
        if (SelectedKey is not { } key || !service.Locs.TryGetValue(key, out var loc))
            return false;
        position = loc.Position;
        orientation = loc.Orientation;
        return true;
    }

    protected override void SetSelectedTransform(Vector3 position, float orientation)
    {
        if (SelectedKey is not { } key || !service.Locs.TryGetValue(key, out var loc))
            return;
        loc.Position = position;
        loc.Orientation = orientation;
        service.NotifyChanged(key);
    }

    protected override void PlaceAt(Vector3 position)
    {
        SelectedKey = service.CreateAt((uint)CurrentMapId, position, 0f);
    }

    protected override void DeleteSelected(uint key) => service.DeleteLoc(key);
}
