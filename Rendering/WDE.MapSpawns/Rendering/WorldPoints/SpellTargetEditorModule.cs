using TheEngine;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.WorldPoints;

namespace WDE.MapSpawns.Rendering.WorldPoints;

/// <summary>
/// The Spell-target tool: every spell_target_position row targeting the current map renders as a
/// beam marker labeled with the spell name; the gizmo moves/rotates the selected one. New rows are
/// armed with a spell id in the inspector, then placed with a world click.
/// </summary>
public class SpellTargetEditorModule : WorldPointModuleBase
{
    private static readonly Vector4 MarkerColor = new(0.70f, 0.40f, 1.00f, 0.95f); // arcane violet

    private readonly ISpellTargetEditorService service;
    private readonly SpellTargetInspector inspector;
    private bool loadInFlight;

    /// <summary>Spell id the armed placement will create a row for (set by the inspector).</summary>
    public uint PendingSpellId { get; set; }

    public ISpellTargetEditorService Service => service;

    public SpellTargetEditorModule(Engine engine,
        IGameContext gameContext,
        ISpawnEditorToolService toolService,
        IWorldInteractionService interaction,
        IInputManager inputManager,
        IGameViewOverlayService overlays,
        RaycastSystem raycastSystem,
        ISpellTargetEditorService service,
        EntryPickerService entryPicker)
        : base(engine, gameContext, toolService, interaction, inputManager, overlays, raycastSystem)
    {
        this.service = service;
        inspector = new SpellTargetInspector(service, this, gameContext, entryPicker);
    }

    protected override SpawnEditorTool Tool => SpawnEditorTool.SpellTarget;
    protected override int GizmoId => 3;
    protected override IInspectorSection Section => inspector;

    // global data, loaded once - unsaved edits survive map switches
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

    public string DescribeSpell(uint spellId) =>
        service.GetSpellName(spellId) is { } name ? $"{name} ({spellId})" : $"Spell {spellId}";

    protected override int DataRevision => service.Revision;

    protected override void CollectPoints(List<WorldPoint> output)
    {
        var map = (uint)CurrentMapId;
        foreach (var row in service.Positions.Values)
        {
            if (row.Map != map)
                continue;
            output.Add(new WorldPoint
            {
                Key = row.SpellId,
                Position = row.Position,
                Orientation = row.Orientation,
                Label = DescribeSpell(row.SpellId),
                Color = MarkerColor,
            });
        }
    }

    protected override bool TryGetSelectedTransform(out Vector3 position, out float orientation)
    {
        position = default;
        orientation = 0;
        if (SelectedKey is not { } key || !service.Positions.TryGetValue(key, out var row))
            return false;
        position = row.Position;
        orientation = row.Orientation;
        return true;
    }

    protected override void SetSelectedTransform(Vector3 position, float orientation)
    {
        if (SelectedKey is not { } key || !service.Positions.TryGetValue(key, out var row))
            return;
        row.Position = position;
        row.Orientation = orientation;
        service.NotifyChanged(key);
    }

    protected override void PlaceAt(Vector3 position)
    {
        if (PendingSpellId == 0)
            return;

        // one destination per spell: creates the row, or just selects the existing one
        service.CreateForSpell(PendingSpellId, (uint)CurrentMapId, position, 0f);
        SelectedKey = PendingSpellId;
        PendingSpellId = 0;
    }

    protected override void DeleteSelected(uint key) => service.DeletePosition(key);
}
