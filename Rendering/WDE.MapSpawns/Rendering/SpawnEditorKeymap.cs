using System.Numerics;
using Hexa.NET.ImGui;
using TheEngine;
using TheEngine.Input;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.CreatureLinking;
using WDE.MapSpawns.Models.Formations;
using WDE.MapSpawns.Models.Pools;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.Models.WorldPoints;
using WDE.MapSpawns.ViewModels;
using IInputManager = TheEngine.Interfaces.IInputManager;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace WDE.MapSpawns.Rendering;

/// <summary>One row of the keymap: what to press, what it does, where it applies. Rows with an
/// <see cref="Execute"/> also appear in the command palette (F3) and run from there; rows without
/// one are documentation for interactions that live in their own handlers (drags, hold-modifiers).
/// </summary>
public sealed class KeyBinding
{
    public required string Keys { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Func<bool>? Enabled { get; init; }
    public Action? Execute { get; init; }
}

/// <summary>
/// The spawn editor's declarative keymap - the single source of truth the shortcut cheat sheet
/// (F1) and the command palette (F3 / Ctrl+P) render from, and the dispatcher for the GLOBAL
/// hotkeys (tool digits, save/undo/redo, duplicate, frame-selected, add-spawn). Tool-local keys
/// (G/R/Del inside the modules, X/Y/Z during a grab) stay in their handlers but are documented
/// here, so the overlay can never drift from a key nobody remembered to list.
/// Owned by <see cref="SpawnViewer"/> (which drives <see cref="HandleHotkeys"/> every frame and
/// attaches the toolbar/picker/placement after construction - they are game-scope singletons by
/// convention of single resolution, not by container registration).
/// </summary>
public class SpawnEditorKeymap
{
    private readonly Engine engine;
    private readonly IInputManager inputManager;
    private readonly ISpawnEditorToolService toolService;
    private readonly IWorldSpawnEditService editService;
    private readonly ISpawnSelectionService selection;
    private readonly IWorldInteractionService interaction;
    private readonly IGameContext gameContext;
    private readonly IFormationEditorService formationService;
    private readonly ISpawnGroupEditorService spawnGroupService;
    private readonly IPoolEditorService poolService;
    private readonly ISafeLocEditorService safeLocService;
    private readonly ISpellTargetEditorService spellTargetService;
    private readonly ICreatureLinkEditorService linkService;
    private readonly SpawnEditorTutorial tutorial;

    private GameViewToolbar? toolbar;
    private SpawnPickerWindow? picker;
    private SpawnPlacementController? placement;

    private bool cheatSheetOpen;
    private bool paletteOpen;
    private bool paletteJustOpened;
    private string paletteFilter = "";
    private int paletteIndex;
    private readonly List<KeyBinding> paletteMatches = new();

    private readonly List<(string group, List<KeyBinding> bindings)> groups = new();

    public SpawnEditorKeymap(Engine engine,
        IInputManager inputManager,
        ISpawnEditorToolService toolService,
        IWorldSpawnEditService editService,
        ISpawnSelectionService selection,
        IWorldInteractionService interaction,
        IGameContext gameContext,
        IFormationEditorService formationService,
        ISpawnGroupEditorService spawnGroupService,
        IPoolEditorService poolService,
        ISafeLocEditorService safeLocService,
        ISpellTargetEditorService spellTargetService,
        ICreatureLinkEditorService linkService,
        SpawnEditorTutorial tutorial)
    {
        this.engine = engine;
        this.inputManager = inputManager;
        this.toolService = toolService;
        this.editService = editService;
        this.selection = selection;
        this.interaction = interaction;
        this.gameContext = gameContext;
        this.formationService = formationService;
        this.spawnGroupService = spawnGroupService;
        this.poolService = poolService;
        this.safeLocService = safeLocService;
        this.spellTargetService = spellTargetService;
        this.linkService = linkService;
        this.tutorial = tutorial;

        BuildBindings();
    }

    /// <summary>The palette needs the toolbar (save) and the picker/placement (add/duplicate);
    /// they are constructed alongside this class, so they arrive after the constructor.</summary>
    public void Attach(GameViewToolbar toolbar, SpawnPickerWindow picker, SpawnPlacementController placement)
    {
        this.toolbar = toolbar;
        this.picker = picker;
        this.placement = placement;
    }

    /// <summary>True while the cheat sheet, the palette or the quick tour is open - Escape then
    /// belongs to them, not to the step-out chain.</summary>
    public bool AnyOverlayOpen => cheatSheetOpen || paletteOpen || tutorial.IsOpen;

    public void OpenCheatSheet() => cheatSheetOpen = true;

    public void OpenPalette()
    {
        paletteOpen = true;
        paletteJustOpened = true;
        paletteFilter = "";
        paletteIndex = 0;
    }

    // ---- global hotkey dispatch --------------------------------------------------------------

    /// <summary>Game loop, every frame, before the tool modules run. ImGui text fields can't
    /// trigger anything here - the ImGui controller clears the engine key queue whenever ImGui
    /// wants the keyboard.</summary>
    public void HandleHotkeys()
    {
        var kb = inputManager.Keyboard;
        bool ctrl = kb.IsDown(Key.LeftCtrl) || kb.IsDown(Key.RightCtrl) || kb.IsDown(Key.LWin) || kb.IsDown(Key.RWin);
        bool shift = kb.IsDown(Key.LeftShift) || kb.IsDown(Key.RightShift);

        if (kb.JustPressed(Key.F1))
            cheatSheetOpen = !cheatSheetOpen;
        if (kb.JustPressed(Key.F3) || (ctrl && kb.JustPressed(Key.P)))
        {
            if (paletteOpen)
                paletteOpen = false;
            else
                OpenPalette();
        }
        if (AnyOverlayOpen && kb.JustPressed(Key.Escape))
        {
            cheatSheetOpen = false;
            paletteOpen = false;
            tutorial.Close();
        }

        // an in-flight grab (typed digits!) or placement owns the keyboard for everything below
        bool busy = interaction.IsCaptured || (placement?.IsPlacing ?? false);

        if (ctrl)
        {
            if (kb.JustPressed(Key.S))
                toolbar?.RequestSaveAll();
            else if (kb.JustPressed(Key.Z) && editService.IsAvailable && !busy)
            {
                if (shift)
                    editService.Redo();
                else
                    editService.Undo();
            }
            else if (kb.JustPressed(Key.Y) && editService.IsAvailable && !busy)
                editService.Redo();
            else if (kb.JustPressed(Key.D) && !busy)
                DuplicateSelected();
            return;
        }

        if (busy)
            return;

        for (int digit = 1; digit <= 8; digit++)
        {
            if (kb.JustPressed((Key)((int)Key.D1 + digit - 1)))
            {
                SelectTool(digit);
                break;
            }
        }

        if (kb.JustPressed(Key.F))
            FrameSelected();
        if (shift && kb.JustPressed(Key.A) && editService.IsAvailable)
            picker?.OpenFor(creatures: true);
    }

    /// <summary>Tool hotkeys 1-8 (toolbar order); unsupported tools are ignored, exactly like
    /// their disabled toolbar buttons.</summary>
    public void SelectTool(int digit)
    {
        var (tool, supported) = digit switch
        {
            1 => (SpawnEditorTool.Select, true),
            2 => (SpawnEditorTool.Waypoint, true),
            3 => (SpawnEditorTool.Formation, formationService.IsSupported),
            4 => (SpawnEditorTool.SpawnGroup, spawnGroupService.IsSupported),
            5 => (SpawnEditorTool.Pool, poolService.IsSupported),
            6 => (SpawnEditorTool.Graveyard, safeLocService.IsSupported),
            7 => (SpawnEditorTool.SpellTarget, spellTargetService.IsSupported),
            8 => (SpawnEditorTool.CreatureLink, linkService.IsSupported),
            _ => (SpawnEditorTool.Select, false),
        };
        if (supported)
            toolService.ActiveTool = tool;
    }

    /// <summary>F - glide the camera to the selected spawn (framed from a few yards back).</summary>
    private void FrameSelected()
    {
        if (selection.SelectedSpawn.Value is { IsSpawned: true } spawn)
            gameContext.CameraManager.Relocate(spawn.WorldObject?.Position ?? spawn.Position, flyHere: true);
    }

    private void DuplicateSelected()
    {
        if (editService.IsAvailable && toolService.ActiveTool == SpawnEditorTool.Select &&
            selection.SelectedSpawn.Value is { IsSpawned: true } spawn)
            placement?.BeginDuplicate(spawn is CreatureSpawnInstance, spawn.Entry, spawn.Guid);
    }

    // ---- the keymap data ----------------------------------------------------------------------

    private void BuildBindings()
    {
        bool HasSelection() => selection.SelectedSpawn.Value is { IsSpawned: true };
        bool EditAvailable() => editService.IsAvailable;

        groups.Add(("General", new List<KeyBinding>
        {
            new() { Keys = "1", Name = "Select tool", Execute = () => SelectTool(1) },
            new() { Keys = "2", Name = "Waypoints tool", Execute = () => SelectTool(2) },
            new() { Keys = "3", Name = "Formations tool", Enabled = () => formationService.IsSupported, Execute = () => SelectTool(3) },
            new() { Keys = "4", Name = "Spawn groups tool", Enabled = () => spawnGroupService.IsSupported, Execute = () => SelectTool(4) },
            new() { Keys = "5", Name = "Pools tool", Enabled = () => poolService.IsSupported, Execute = () => SelectTool(5) },
            new() { Keys = "6", Name = "Graveyards tool", Enabled = () => safeLocService.IsSupported, Execute = () => SelectTool(6) },
            new() { Keys = "7", Name = "Spell targets tool", Enabled = () => spellTargetService.IsSupported, Execute = () => SelectTool(7) },
            new() { Keys = "8", Name = "Creature linking tool", Enabled = () => linkService.IsSupported, Execute = () => SelectTool(8) },
            new() { Keys = "Esc", Name = "Step out: back to the Select tool, then deselect", Description = "Innermost thing first: an open popup, a grab, a placement, pen mode, the point selection" },
            new() { Keys = "Ctrl+S", Name = "Save all 3D editors", Execute = () => toolbar?.RequestSaveAll() },
            new() { Keys = "Ctrl+Z", Name = "Undo spawn edit", Enabled = EditAvailable, Execute = () => editService.Undo() },
            new() { Keys = "Ctrl+Shift+Z / Ctrl+Y", Name = "Redo spawn edit", Enabled = EditAvailable, Execute = () => editService.Redo() },
            new() { Keys = "Shift+A", Name = "Add a spawn (opens the picker)", Enabled = EditAvailable, Execute = () => picker?.OpenFor(creatures: true) },
            new() { Keys = "F1", Name = "Keyboard shortcuts overlay", Execute = () => cheatSheetOpen = true },
            new() { Keys = "F3 / Ctrl+P", Name = "Command palette", Execute = OpenPalette },
            new() { Keys = "", Name = "Quick tour", Execute = () => tutorial.Open() },
        }));

        groups.Add(("Camera", new List<KeyBinding>
        {
            new() { Keys = "W / A / S / D", Name = "Fly forward / left / back / right" },
            new() { Keys = "E / Q", Name = "Fly up / down" },
            new() { Keys = "hold Shift", Name = "Fly fast" },
            new() { Keys = "hold N", Name = "Fly slow" },
            new() { Keys = "RMB drag", Name = "Look around" },
            new() { Keys = "Wheel", Name = "Zoom (dolly along the view)" },
            new() { Keys = "Wheel while RMB", Name = "Change the fly speed", Description = "The multiplier sticks; shown next to the coordinates" },
            new() { Keys = "MMB drag", Name = "Orbit", Description = "Around the selected spawn when it is in view, else around what the camera looks at" },
            new() { Keys = "Shift+MMB drag", Name = "Pan" },
            new() { Keys = "F", Name = "Fly to the selected spawn", Enabled = HasSelection, Execute = FrameSelected },
        }));

        groups.Add(("Select tool", new List<KeyBinding>
        {
            new() { Keys = "Click", Name = "Select a spawn (empty ground: deselect)" },
            new() { Keys = "Double-click", Name = "Edit the spawn's table row" },
            new() { Keys = "Right-click", Name = "Context menu / add a creature or gameobject" },
            new() { Keys = "G", Name = "Grab the selected spawn", Description = "G again mid-grab: snap to the nearest surface" },
            new() { Keys = "R", Name = "Rotate the selected spawn (faces the cursor)" },
            new() { Keys = "Del", Name = "Mark for deletion (Del again restores)", Enabled = () => EditAvailable() && HasSelection() },
            new() { Keys = "Ctrl+D", Name = "Duplicate the selected spawn", Enabled = () => EditAvailable() && HasSelection(), Execute = DuplicateSelected },
        }));

        groups.Add(("While grabbing / rotating", new List<KeyBinding>
        {
            new() { Keys = "X / Y / Z", Name = "Lock the move to a world axis (same key: unlock)" },
            new() { Keys = "type digits", Name = "Exact distance (yards) or rotation (degrees)", Description = "Backspace edits, Enter or click applies" },
            new() { Keys = "hold Ctrl", Name = "Snap: 0.5 yd grid / 15° steps" },
            new() { Keys = "hold Shift", Name = "Precision (slow) motion" },
            new() { Keys = "hold Alt", Name = "Raise / lower instead of sliding" },
            new() { Keys = "Click / Enter", Name = "Drop (commit)" },
            new() { Keys = "Esc", Name = "Cancel and restore the old transform" },
        }));

        groups.Add(("Placing a new spawn", new List<KeyBinding>
        {
            new() { Keys = "Click, drag, release", Name = "Pin the spawn, aim its facing, commit" },
            new() { Keys = "hold Shift", Name = "Keep placing more after the drop" },
            new() { Keys = "Esc / right-click", Name = "Cancel the placement" },
        }));

        groups.Add(("Waypoints tool", new List<KeyBinding>
        {
            new() { Keys = "P", Name = "Pen mode: click terrain to append points (Esc leaves)" },
            new() { Keys = "Click a line", Name = "Insert a point on that segment (and drag it)" },
            new() { Keys = "G", Name = "Grab the selected point" },
            new() { Keys = "Del", Name = "Delete the selected point" },
            new() { Keys = "Double-click a point", Name = "All the point's properties (popover)" },
            new() { Keys = "Esc", Name = "Leave pen mode, then deselect the point, then back to Select" },
        }));

        groups.Add(("Formations & creature links", new List<KeyBinding>
        {
            new() { Keys = "Drag creature → creature", Name = "Create the link (member → leader / slave → master)" },
            new() { Keys = "Click an arrow", Name = "Select the link" },
            new() { Keys = "Del", Name = "Remove the selected link" },
        }));

        groups.Add(("Spawn groups & pools", new List<KeyBinding>
        {
            new() { Keys = "Click a member", Name = "Select its group / pool" },
            new() { Keys = "Click an ungrouped spawn", Name = "Toggle it into the pending selection (green)" },
        }));

        groups.Add(("Panels", new List<KeyBinding>
        {
            new() { Keys = "Double-click a tree row", Name = "Fly the camera there (loads the map if needed)" },
            new() { Keys = "Enter in the tree", Name = "Fly to the highlighted row" },
            new() { Keys = "", Name = "Gizmo: Move", Enabled = () => toolService.ActiveTool.Supports(GizmoMode.Translate), Execute = () => toolService.GizmoMode = GizmoMode.Translate },
            new() { Keys = "", Name = "Gizmo: Rotate", Enabled = () => toolService.ActiveTool.Supports(GizmoMode.Rotate), Execute = () => toolService.GizmoMode = GizmoMode.Rotate },
            new() { Keys = "", Name = "Toggle the on-screen gizmo handles", Description = "G/R grabs keep working - for keyboard-first editing", Execute = () => toolService.ShowGizmoHandles = !toolService.ShowGizmoHandles },
        }));
    }

    // ---- rendering ------------------------------------------------------------------------------

    public void RenderGUI()
    {
        DrawCheatSheet();
        DrawPalette();
    }

    private void DrawCheatSheet()
    {
        if (!cheatSheetOpen)
            return;

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + viewport.Size * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(720, MathF.Min(640, viewport.Size.Y * 0.85f)), ImGuiCond.Appearing);
        if (!ImGui.Begin("Keyboard shortcuts", ref cheatSheetOpen, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse))
        {
            ImGui.End();
            return;
        }

        ImGui.TextDisabled("F1 toggles this overlay · every key the 3D spawn editor understands");
        ImGui.Separator();

        foreach (var (group, bindings) in groups)
        {
            ImGui.SeparatorText(group);
            if (ImGui.BeginTable($"##keys_{group}", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("keys", ImGuiTableColumnFlags.WidthFixed, 190f);
                ImGui.TableSetupColumn("action");
                foreach (var b in bindings)
                {
                    if (b.Keys.Length == 0)
                        continue; // palette-only commands aren't keyboard rows
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(new Vector4(1f, 0.85f, 0.45f, 1f), b.Keys);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(b.Name);
                    if (b.Description != null)
                        ImGui.TextDisabled(b.Description);
                }
                ImGui.EndTable();
            }
        }

        ImGui.Separator();
        if (ImGui.Button("Close", new Vector2(120, 0)))
            cheatSheetOpen = false;
        ImGui.SameLine();
        ImGui.TextDisabled("Esc closes too");
        ImGui.End();
    }

    private void DrawPalette()
    {
        if (!paletteOpen)
            return;

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X + viewport.Size.X * 0.5f, viewport.Pos.Y + viewport.Size.Y * 0.22f),
            ImGuiCond.Always, new Vector2(0.5f, 0f));
        ImGui.SetNextWindowSize(new Vector2(460, 0), ImGuiCond.Always);
        if (!ImGui.Begin("##command_palette", ref paletteOpen,
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
        {
            ImGui.End();
            return;
        }

        if (paletteJustOpened)
            ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(-1);
        bool submitted = ImGui.InputTextWithHint("##palette_filter", "Type a command...", ref paletteFilter, 128,
            ImGuiInputTextFlags.EnterReturnsTrue);
        paletteJustOpened = false;

        // filtered, executable commands only (display-only rows can't run from here)
        paletteMatches.Clear();
        foreach (var (_, bindings) in groups)
        {
            foreach (var b in bindings)
            {
                if (b.Execute == null)
                    continue;
                if (paletteFilter.Length > 0 &&
                    b.Name.IndexOf(paletteFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                paletteMatches.Add(b);
            }
        }

        if (paletteMatches.Count == 0)
        {
            ImGui.TextDisabled("No matching command");
        }
        else
        {
            paletteIndex = Math.Clamp(paletteIndex, 0, paletteMatches.Count - 1);
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                paletteIndex = (paletteIndex + 1) % paletteMatches.Count;
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                paletteIndex = (paletteIndex - 1 + paletteMatches.Count) % paletteMatches.Count;

            float rowHeight = ImGui.GetTextLineHeightWithSpacing();
            if (ImGui.BeginChild("##palette_list", new Vector2(0, MathF.Min(paletteMatches.Count, 10.5f) * rowHeight + 8)))
            {
                for (int i = 0; i < paletteMatches.Count; ++i)
                {
                    var b = paletteMatches[i];
                    bool enabled = b.Enabled?.Invoke() ?? true;
                    if (!enabled)
                        ImGui.BeginDisabled();
                    if (ImGui.Selectable($"{b.Name}##pal{i}", i == paletteIndex))
                    {
                        paletteIndex = i;
                        RunPaletteCommand(b);
                    }
                    if (!enabled)
                        ImGui.EndDisabled();
                    if (b.Keys.Length > 0)
                    {
                        ImGui.SameLine(MathF.Max(0, ImGui.GetWindowWidth() - ImGui.CalcTextSize(b.Keys).X - 16));
                        ImGui.TextDisabled(b.Keys);
                    }
                }
            }
            ImGui.EndChild();

            if (submitted)
                RunPaletteCommand(paletteMatches[paletteIndex]);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            paletteOpen = false;

        ImGui.End();
    }

    private void RunPaletteCommand(KeyBinding binding)
    {
        if (binding.Enabled?.Invoke() ?? true)
        {
            paletteOpen = false;
            binding.Execute?.Invoke();
        }
    }
}
