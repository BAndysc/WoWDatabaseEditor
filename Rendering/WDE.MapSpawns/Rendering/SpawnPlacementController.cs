using System.Threading.Tasks;
using TheEngine;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheEngine.Structures;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;
using WDE.Module.Attributes;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// "Phantom" placement: after a template is chosen in the picker, a dithered ghost model follows the
/// cursor (raycast onto the world). Sims-style commit: pressing the left button pins the phantom at
/// that spot; while the button is HELD, the phantom rotates around Z to face the cursor; releasing
/// commits the spawn with that orientation (no drag = default facing). Holding Shift keeps placing
/// more, otherwise placement ends on release. Escape / right click cancels.
/// Driven by <see cref="SpawnViewer"/>.
///
/// NOT [AutoRegister]: it depends on <see cref="IGameContext"/>, which only exists in the per-game
/// child container. SpawnViewer (a game module resolved in that scope) owns the single instance and
/// hands it to the picker via <see cref="SpawnPickerWindow.AttachPlacement"/>.
/// </summary>
public class SpawnPlacementController
{
    private readonly IGameContext gameContext;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly IInputManager inputManager;
    private readonly RaycastSystem raycastSystem;
    private readonly IWorldSpawnEditService editService;
    private readonly IWorldInteractionService interaction;

    private RenderLayer renderLayer;
    private bool initialized;

    private bool isCreature;
    private uint entry;
    private uint duplicateSourceGuid; // != 0: commit clones this spawn's row instead of a bare create
    private WorldObjectInstance? phantom;
    private int generation; // invalidates an in-flight load if placement changed/ended

    // Sims-style rotate-while-held: button down pins the phantom at the anchor, dragging rotates it
    // to face the cursor, release commits
    private bool rotating;
    private Vector3 anchor;
    private float orientation;
    // Shift held at ANY point of the pin-drag keeps placing (not only at the exact release frame)
    private bool keepPlacing;
    // the cursor must leave this radius (yd) around the anchor before the facing starts following it
    private const float RotateDeadZone = 0.6f;

    public bool IsPlacing { get; private set; }

    /// <summary>Placement started but the phantom model hasn't finished loading - clicks are
    /// ignored during this window, so the UI shows a cursor-attached "loading" marker.</summary>
    public bool IsLoadingPhantom => IsPlacing && phantom == null;

    private string phantomName = "";

    /// <summary>One-line placement status for the hint bar (what is being placed, live ghost
    /// coordinates, facing while dragging, and the controls). Null when not placing.</summary>
    public string? StatusHint { get; private set; }

    public SpawnPlacementController(IGameContext gameContext,
        ICachedDatabaseProvider databaseProvider,
        IInputManager inputManager,
        RaycastSystem raycastSystem,
        IWorldSpawnEditService editService,
        IWorldInteractionService interaction)
    {
        this.gameContext = gameContext;
        this.databaseProvider = databaseProvider;
        this.inputManager = inputManager;
        this.raycastSystem = raycastSystem;
        this.editService = editService;
        this.interaction = interaction;
    }

    public void Initialize(RenderLayer layer)
    {
        renderLayer = layer;
        initialized = true;
    }

    public void Begin(bool isCreature, uint entry)
    {
        if (!initialized)
            return;
        Cancel();
        this.isCreature = isCreature;
        this.entry = entry;
        IsPlacing = true;
        LoadPhantom(++generation).ListenErrors();
    }

    /// <summary>Same phantom flow as <see cref="Begin"/>, but committing duplicates the source
    /// spawn's full row (new guid, this position/facing) instead of creating a bare new spawn.</summary>
    public void BeginDuplicate(bool isCreature, uint entry, uint sourceGuid)
    {
        Begin(isCreature, entry);
        duplicateSourceGuid = sourceGuid;
    }

    public void Cancel()
    {
        generation++;
        IsPlacing = false;
        rotating = false;
        keepPlacing = false;
        orientation = 0f;
        duplicateSourceGuid = 0;
        phantomName = "";
        StatusHint = null;
        phantom?.Dispose();
        phantom = null;
    }

    public void Dispose() => Cancel();

    public void Update(float delta)
    {
        if (!IsPlacing)
            return;

        if (inputManager.Keyboard.JustPressed(Key.Escape) || inputManager.Mouse.HasJustClicked(MouseButton.Right))
        {
            if (inputManager.Mouse.HasJustClicked(MouseButton.Right))
                interaction.UsePointerThisFrame(); // the cancel click shouldn't also right-click-select
            Cancel();
            return;
        }

        var ray = gameContext.Engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
        var hit = raycastSystem.Raycast(ray, null, false, Collisions.COLLISION_MASK_STATIC);

        UpdateStatusHint(hit?.Item2);

        // no ImGui WantCaptureMouse check: it is always true over the viewport (the game view is an
        // ImGui window). Clicks over real UI windows never reach us anyway - ImGuiController eats
        // mouse input when ImGui wants the mouse and the game view is not hovered.
        if (!rotating)
        {
            // follow the cursor; pressing the button pins the phantom and starts the facing drag
            if (hit.HasValue && phantom != null)
                phantom.Position = hit.Value.Item2;

            if (phantom != null && hit.HasValue && inputManager.Mouse.HasJustClicked(MouseButton.Left))
            {
                interaction.UsePointerThisFrame(); // don't let the same click select a spawn below
                rotating = true;
                anchor = hit.Value.Item2;
                keepPlacing = false;
                // orientation intentionally KEEPS the previous placement's facing - a row of guards
                // placed with Shift all face the same way without re-doing the drag each time
            }
            return;
        }

        // button held: rotate the pinned phantom to face the cursor (Sims-style)
        interaction.UsePointerThisFrame();
        keepPlacing |= inputManager.Keyboard.IsDown(Key.LeftShift) || inputManager.Keyboard.IsDown(Key.RightShift);
        if (phantom != null)
        {
            phantom.Position = anchor;
            if (hit.HasValue)
            {
                var toCursor = hit.Value.Item2 - anchor;
                if (toCursor.X * toCursor.X + toCursor.Y * toCursor.Y > RotateDeadZone * RotateDeadZone)
                    orientation = MathF.Atan2(toCursor.Y, toCursor.X);
            }
            SetPhantomOrientation(orientation);
        }

        if (!inputManager.Mouse.RawIsMouseDown(MouseButton.Left))
        {
            // released -> commit (raw state: a release outside the view must still finish the drag)
            rotating = false;
            Console.WriteLine($"[SpawnPlacement] placing {(isCreature ? "creature" : "gameobject")} {entry} at {anchor}, o={orientation:0.###}");
            if (duplicateSourceGuid != 0)
                editService.DuplicateSpawn(isCreature, duplicateSourceGuid, entry, (int)gameContext.CurrentMapId, anchor, orientation);
            else
                editService.CreateSpawn(isCreature, entry, (int)gameContext.CurrentMapId, anchor, orientation);
            keepPlacing |= inputManager.Keyboard.IsDown(Key.LeftShift) || inputManager.Keyboard.IsDown(Key.RightShift);
            if (!keepPlacing)
                Cancel(); // single placement done; Shift (at any point of the drag) keeps the phantom for more
            else
                SetPhantomOrientation(orientation); // next phantom keeps the same facing
        }
    }

    private void UpdateStatusHint(Vector3? cursorHit)
    {
        string what = string.IsNullOrEmpty(phantomName)
            ? $"{(isCreature ? "creature" : "gameobject")} {entry}"
            : $"{phantomName} ({entry})";
        if (duplicateSourceGuid != 0)
            what = $"copy of {what}";

        if (rotating)
        {
            float rad = orientation;
            if (rad < 0)
                rad += MathF.Tau;
            StatusHint = $"Placing {what} · facing {rad:0.00} rad ({MathUtil.RadiansToDegrees(rad):0}°) · release: place · Shift: keep placing";
        }
        else if (phantom == null)
        {
            // clicks are ignored until the model is in - say so instead of a silent dead zone
            StatusHint = $"Placing {what} · loading the model... · Esc/RMB: cancel";
        }
        else
        {
            var p = cursorHit ?? phantom?.Position;
            string at = p is { } pos ? $" at {pos.X:0.#}  {pos.Y:0.#}  {pos.Z:0.#}" : "";
            StatusHint = $"Placing {what}{at} · click: place · click-drag: face · Shift: keep placing · Esc/RMB: cancel";
        }
    }

    private void SetPhantomOrientation(float o)
    {
        if (phantom is CreatureInstance creature)
            creature.Orientation = o;
        else if (phantom is GameObjectInstance gameObject)
            gameObject.Rotation = Quaternion.CreateFromAxisAngle(Vectors.Up, o);
    }

    private async Task LoadPhantom(int gen)
    {
        WorldObjectInstance? instance = null;
        string name = "";
        if (isCreature)
        {
            var template = databaseProvider.GetCachedCreatureTemplate(entry) ?? await databaseProvider.GetCreatureTemplate(entry);
            if (template != null)
            {
                name = template.Name;
                var creature = new CreatureInstance(gameContext, template, null, renderLayer);
                await creature.Load();
                instance = creature;
            }
        }
        else
        {
            var template = databaseProvider.GetCachedGameObjectTemplate(entry) ?? await databaseProvider.GetGameObjectTemplate(entry);
            if (template != null)
            {
                name = template.Name;
                var go = new GameObjectInstance(gameContext, template, null, renderLayer);
                await go.Load();
                instance = go;
            }
        }

        // placement was cancelled / restarted while loading
        if (gen != generation || instance == null)
        {
            instance?.Dispose();
            return;
        }

        phantom = instance;
        phantomName = name;
        phantom.SetTranslucent(true); // ghost dither (safe: instances own per-instance material clones)
    }
}
