using TheEngine;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Waypoints;
using WDE.QueryGenerators.Base;

namespace WDE.MapSpawns.Rendering.Waypoints;

/// <summary>
/// The Waypoint tool's inspector section: a header for the one open path (name/dirty/load/close),
/// the selected point's editable properties and - demoted to a collapsed expander - the full point
/// table. Plus the popups: the load picker, the SQL preview and the per-point properties popover
/// (non-modal, anchored where the user double-clicked). The editor works on a single path at a time;
/// opening another path stages the previous one's unsaved edits (pending) rather than prompting.
/// Pure UI - state lives in <see cref="IWaypointEditorService"/>, world interaction in
/// <see cref="WaypointEditorModule"/>.
/// </summary>
public sealed class WaypointInspector : IInspectorSection
{
    private readonly IWaypointEditorService service;
    private readonly ISpawnScriptsService scriptsService;
    private readonly IGameNotificationService notifications;

    // the point waiting for a suggested movement script id from the bridge ("New script" flow)
    private EditablePath? suggestScriptPath;
    private int suggestScriptIndex = -1;

    // load picker state
    private bool openLoadPopup;
    private WaypointSource loadSource;
    private Task<IReadOnlyList<uint>>? loadIdsTask;
    private Task<IReadOnlyList<(uint key, uint key2)>>? loadCompoundTask; // compound-keyed sources only
    private int newPathId;
    private int newPathId2; // secondary key (PathId) for compound sources
    private string loadFilter = "";

    // generate-sql preview state

    // per-waypoint properties popover state
    private bool openPropsPopup;
    private Vector2 propsAnchor;
    private EditablePath? propsPath;
    private int propsIndex = -1;

    public WaypointInspector(IWaypointEditorService service, ISpawnScriptsService scriptsService,
        IGameNotificationService notifications)
    {
        this.service = service;
        this.scriptsService = scriptsService;
        this.notifications = notifications;
        loadSource = service.AvailableSources.Count > 0 ? service.AvailableSources[0] : WaypointSource.TrinityWaypointData;
    }

    public string Title => "Waypoints";

    // "dirty" for the section covers all pending waypoint edits (the open path + any staged ones),
    // so the section badge and the unified Save reflect everything still unsaved
    public bool IsDirty => service.AnyDirty;

    public Func<Task>? SaveSelf => service.AnyDirty ? service.SaveAllDirty : null;

    // Revert only the open path (reload it from the DB) - the visible, undoable action
    public Func<Task>? RevertSelf => service.SelectedPath is { IsDirty: true } ? RevertCurrent : null;

    private Task RevertCurrent()
    {
        if (service.SelectedPath is not { } path)
            return Task.CompletedTask;
        var source = path.Source;
        var key = path.Key;
        var key2 = path.Key2;
        var autoGuid = path.AutoLoadedFromCreatureGuid;
        service.Unload(path);
        // a never-saved path has no DB rows - LoadPath returns null and it simply disappears
        return service.LoadPath(source, key, autoGuid, key2);
    }

    /// <summary>Set by the module every frame from its point dragger (this class has no module ref).</summary>
    public string? DragHint;

    public string? Hints
    {
        get
        {
            if (service.AvailableSources.Count == 0)
                return "The current core has no editable waypoint tables";
            if (DragHint != null)
                return DragHint;
            if (service.SelectedPath == null)
                return "Load a path, or right-click a creature → Edit waypoints";
            if (service.EditingPath != null)
                return "ADDING POINTS — click terrain: add · click line: insert · Esc/P: stop";
            return "Click a point: select · double-click: properties · click a line: insert · G grab · Del delete · P: add points";
        }
    }

    /// <summary>Opens the point property popover, anchored at the current mouse position (i.e. at
    /// the double-clicked table row or world point).</summary>
    public void OpenProperties(EditablePath path, int index)
    {
        propsPath = path;
        propsIndex = index;
        propsAnchor = ImGui.GetMousePos();
        openPropsPopup = true;
    }

    public void DrawContent()
    {
        if (service.AvailableSources.Count == 0)
        {
            ImGui.TextDisabled("The current core has no editable\nwaypoint tables.");
            return;
        }

        DrawPathHeader();

        if (service.SelectedPath is not { } path)
        {
            ImGui.TextDisabled("No path open.");
            ImGui.TextDisabled("Load a path here, or right-click a\ncreature and pick \"Edit waypoints\".");
            return;
        }

        ImGui.TextDisabled($"{path.Points.Count} point{(path.Points.Count == 1 ? "" : "s")}{(path.IsDirty ? "  ·  unsaved" : "")}");

        // the explicit pen-mode gate: while armed, world clicks add points to THIS path;
        // otherwise clicks only select/inspect
        bool editing = ReferenceEquals(service.EditingPath, path);
        if (editing)
        {
            EditorTheme.PushArmedButton();
            if (ImGui.Button("Adding points — click terrain  (Esc)", new Vector2(-1, 0)))
                service.EditingPath = null;
            EditorTheme.PopButtonColors();
        }
        else if (ImGui.Button("Add points  (P)", new Vector2(-1, 0)))
            service.EditingPath = path;

        if (ImGui.SmallButton("Close"))
        {
            // stop editing this path; unsaved edits stay pending (saved by the unified Save)
            service.SelectedPath = null;
            return;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(path.IsDirty
                ? "Stop editing. The unsaved changes stay pending and are written by Save."
                : "Stop editing this path.");

        DrawPathLevelProperties(path);

        DrawSelectedPoint(path);

        if (ImGui.CollapsingHeader("All points"))
            DrawPointsTable(path);
    }

    /// <summary>The popups must draw even when the panel content doesn't (another tool active, a
    /// world double-click...) - called unconditionally from the module's RenderGUI.</summary>
    public void DrawPopups()
    {
        ConsumeSuggestedScriptId();
        DrawLoadPopup();
        DrawPropertiesPopover();
    }

    /// <summary>Finishes the "New script" flow: when the bridge answers with a free
    /// dbscripts_on_creature_movement id, write it into the waiting point and open the editor.</summary>
    private void ConsumeSuggestedScriptId()
    {
        if (suggestScriptPath == null)
            return;
        if (scriptsService.TakeMovementScriptIdSuggestion() is not { } id)
            return;
        var path = suggestScriptPath;
        var index = suggestScriptIndex;
        suggestScriptPath = null;
        suggestScriptIndex = -1;
        if (index < 0 || index >= path.Points.Count || !service.LoadedPaths.Contains(path))
            return;
        var p = path.Points[index];
        p.ScriptId = id;
        path.Points[index] = p;
        path.MarkDirty();
        scriptsService.OpenMovementScript(id);
    }

    // one path at a time: just the open path's name (or "no path") + a Load button. Loading another
    // path replaces this one, keeping its unsaved edits pending.
    private void DrawPathHeader()
    {
        var current = service.SelectedPath;
        if (ImGui.SmallButton(current == null ? "Load..." : "Load"))
        {
            openLoadPopup = true;
            StartEnumerate();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(current == null
                ? "Open a waypoint path for editing"
                : "Open a different path (this one's unsaved edits stay pending)");
        ImGui.SameLine();
        if (current == null)
            ImGui.TextDisabled("(no path open)");
        else
            ImGui.TextUnformatted(current.DisplayName + (current.IsDirty ? "  *" : ""));
    }

    /// <summary>Path-LEVEL data some sources carry next to the points: the cmangos
    /// waypoint_path_name and TC master's waypoint_path metadata row. Only the fields the active
    /// core's table really has are shown (<see cref="IWaypointEditorService.ColumnsFor"/>).</summary>
    private void DrawPathLevelProperties(EditablePath path)
    {
        var columns = service.ColumnsFor(path.Source);

        if (columns.HasFlagFast(WaypointColumns.PathName))
        {
            string name = path.PathName ?? "";
            if (ImGui.InputTextWithHint("Path name", "(no name)", ref name, 128))
                path.UpdatePathName(name);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("waypoint_path_name row for this path - empty removes it");
        }

        if (columns.HasFlagFast(WaypointColumns.PathHeader) && path.Header is { } header)
        {
            ImGui.SeparatorText("Path settings");
            ImGui.TextDisabled("Whole-path properties (the waypoint_path row)");
            bool changed = false;

            int moveType = header.MoveType;
            ImGui.SetNextItemWidth(160);
            if (ImGui.Combo("Move type", ref moveType, "Walk\0Run\0Fly\0"))
            {
                header.MoveType = moveType;
                changed = true;
            }

            int flags = header.Flags;
            ImGui.SetNextItemWidth(160);
            if (ImGui.InputInt("Flags", ref flags))
            {
                header.Flags = flags;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Raw waypoint_path.Flags value");

            bool hasVelocity = header.Velocity.HasValue;
            if (ImGui.Checkbox("Custom velocity", ref hasVelocity))
            {
                header.Velocity = hasVelocity ? (header.Velocity ?? 0f) : null;
                changed = true;
            }
            if (header.Velocity is { } velocity)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.InputFloat("##pathvelocity", ref velocity, 0, 0, "%.2f"))
                {
                    header.Velocity = velocity;
                    changed = true;
                }
            }

            string comment = header.Comment ?? "";
            if (ImGui.InputText("Path comment", ref comment, 256))
            {
                header.Comment = comment;
                changed = true;
            }

            if (changed)
                path.UpdateHeader(header);
        }
    }

    private void DrawSelectedPoint(EditablePath path)
    {
        int idx = service.SelectedPointIndex;
        if (idx < 0 || idx >= path.Points.Count)
        {
            ImGui.SeparatorText("Point");
            ImGui.TextDisabled("Click a point in the world to edit it.");
            return;
        }

        ImGui.SeparatorText($"Point #{idx + 1} of {path.Points.Count}");

        var p = path.Points[idx];
        bool changed = false;
        var columns = service.ColumnsFor(path.Source);

        var pos = new Vector3(p.X, p.Y, p.Z);
        if (ImGui.InputFloat3("Position", ref pos, "%.2f"))
        {
            p.X = pos.X;
            p.Y = pos.Y;
            p.Z = pos.Z;
            changed = true;
        }
        if (columns.HasFlagFast(WaypointColumns.Orientation))
        {
            changed |= FloatNullable("Orientation", ref p, (ref UniversalWaypoint w, float v) => w.Orientation = v, p.Orientation, "%.3f rad");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Facing at this point, radians ({(p.Orientation ?? 0f) * 180f / MathF.PI:0.#}°)");
        }

        changed |= DrawSourceFields(path.Source, columns, ref p, full: false, path, idx);

        if (changed)
        {
            path.Points[idx] = p;
            path.MarkDirty();
        }

        if (ImGui.SmallButton("More..."))
            OpenProperties(path, idx);
        ImGui.SameLine();
        if (ImGui.SmallButton("Delete point"))
            RemovePoint(path, idx);
    }

    private void RemovePoint(EditablePath path, int index)
    {
        path.RemoveAt(index);
        if (service.SelectedPointIndex >= path.Points.Count)
            service.SelectedPointIndex = path.Points.Count - 1;
    }

    // the per-source editable fields - only the columns the active core's table really has
    // (see IWaypointEditorService.ColumnsFor); 'full' additionally shows the rarely-used ones.
    // path/index identify the point for deferred actions (the "New script" id round-trip).
    private bool DrawSourceFields(WaypointSource source, WaypointColumns columns, ref UniversalWaypoint p, bool full, EditablePath path, int index)
    {
        bool changed = false;
        switch (source)
        {
            case WaypointSource.TrinityWaypointData:
                changed |= UInt("Delay (ms)", ref p, (ref UniversalWaypoint w, uint v) => w.Delay = v, p.Delay ?? 0);
                if (columns.HasFlagFast(WaypointColumns.Velocity))
                    changed |= FloatNullable("Velocity", ref p, (ref UniversalWaypoint w, float v) => w.Velocity = v, p.Velocity);
                if (columns.HasFlagFast(WaypointColumns.MoveType))
                    changed |= IntField("Move type", ref p, (ref UniversalWaypoint w, int v) => w.MoveType = v, p.MoveType ?? 0);
                if (columns.HasFlagFast(WaypointColumns.Action))
                    changed |= IntField("Action", ref p, (ref UniversalWaypoint w, int v) => w.Action = v, p.Action ?? 0);
                if (full)
                {
                    if (columns.HasFlagFast(WaypointColumns.SmoothTransition))
                        changed |= Bool("Smooth transition", ref p, (ref UniversalWaypoint w, bool v) => w.SmoothTransition = v, p.SmoothTransition ?? false);
                    if (columns.HasFlagFast(WaypointColumns.ActionChance))
                        changed |= UInt("Action chance", ref p, (ref UniversalWaypoint w, uint v) => w.ActionChance = (byte)Math.Min(v, 255u), p.ActionChance ?? 0);
                }
                break;
            case WaypointSource.SmartScriptWaypoint:
                changed |= UInt("Delay (ms)", ref p, (ref UniversalWaypoint w, uint v) => w.Delay = v, p.Delay ?? 0);
                if (columns.HasFlagFast(WaypointColumns.Velocity))
                    changed |= FloatNullable("Velocity", ref p, (ref UniversalWaypoint w, float v) => w.Velocity = v, p.Velocity);
                if (full && columns.HasFlagFast(WaypointColumns.SmoothTransition))
                    changed |= Bool("Smooth transition", ref p, (ref UniversalWaypoint w, bool v) => w.SmoothTransition = v, p.SmoothTransition ?? false);
                if (columns.HasFlagFast(WaypointColumns.Comment))
                    changed |= Comment(ref p);
                break;
            case WaypointSource.ScriptWaypoint:
                changed |= UInt("Wait time (ms)", ref p, (ref UniversalWaypoint w, uint v) => w.Delay = v, p.Delay ?? 0);
                if (columns.HasFlagFast(WaypointColumns.Comment))
                    changed |= Comment(ref p);
                break;
            case WaypointSource.MangosWaypointPath:
            case WaypointSource.MangosCreatureMovement:
            case WaypointSource.MangosCreatureMovementTemplate:
                changed |= UInt("Wait time (ms)", ref p, (ref UniversalWaypoint w, uint v) => w.Delay = v, p.Delay ?? 0);
                if (columns.HasFlagFast(WaypointColumns.ScriptId))
                {
                    changed |= UInt("Script id", ref p, (ref UniversalWaypoint w, uint v) => w.ScriptId = v, p.ScriptId ?? 0);
                    DrawMovementScriptButtons(in p, path, index);
                }
                if (columns.HasFlagFast(WaypointColumns.Comment))
                    changed |= Comment(ref p);
                break;
        }
        return changed;
    }

    /// <summary>The waypoint's dbscripts_on_creature_movement link: edit the script the id points
    /// to, or (id 0) fetch a free id from the bridge, stamp it on the point and open the editor.</summary>
    private void DrawMovementScriptButtons(in UniversalWaypoint p, EditablePath path, int index)
    {
        if (!scriptsService.IsAvailable)
            return;

        var scriptId = p.ScriptId ?? 0;
        if (scriptId > 0)
        {
            if (ImGui.SmallButton("Edit script##wpscript"))
                scriptsService.OpenMovementScript(scriptId);
            ImGui.SameLine();
            ImGui.TextDisabled("dbscripts_on_creature_movement");
        }
        else
        {
            bool pending = ReferenceEquals(suggestScriptPath, path) && suggestScriptIndex == index;
            if (pending)
                ImGui.TextDisabled("Picking a free script id...");
            else if (ImGui.SmallButton("New script##wpscript"))
            {
                suggestScriptPath = path;
                suggestScriptIndex = index;
                scriptsService.RequestMovementScriptIdSuggestion();
            }
        }
    }

    private void DrawPointsTable(EditablePath path)
    {
        // no orientation column for tables that don't store one (script_waypoint)
        bool hasOrientation = service.ColumnsFor(path.Source).HasFlagFast(WaypointColumns.Orientation);
        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
                         ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("points", hasOrientation ? 6 : 5, tableFlags, new Vector2(0, 180)))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28);
        ImGui.TableSetupColumn("X");
        ImGui.TableSetupColumn("Y");
        ImGui.TableSetupColumn("Z");
        if (hasOrientation)
            ImGui.TableSetupColumn("O (rad)");
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableHeadersRow();

        int removeAt = -1;

        for (int i = 0; i < path.Points.Count; ++i)
        {
            var p = path.Points[i];
            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableNextColumn();
            bool rowSelected = service.SelectedPointIndex == i;
            // the row selectable spans every column, so without AllowOverlap it eats the clicks meant
            // for the X/Y/Z/O input cells and the delete ("x") button drawn on top of it
            ImGui.SetNextItemAllowOverlap();
            if (ImGui.Selectable((i + 1).ToString(), rowSelected, ImGuiSelectableFlags.SpanAllColumns))
                service.SelectedPointIndex = i;
            // double-click the row (its number cell) to open the property popover
            if (ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    service.SelectedPointIndex = i;
                    OpenProperties(path, i);
                }
                else if (ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip))
                    ImGui.SetTooltip("Double-click: point properties (delay, orientation, ...)");
            }

            ImGui.TableNextColumn();
            float x = p.X;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat("##x", ref x, 0, 0, "%.3f")) { p.X = x; path.Points[i] = p; path.MarkDirty(); }

            ImGui.TableNextColumn();
            float y = p.Y;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat("##y", ref y, 0, 0, "%.3f")) { p.Y = y; path.Points[i] = p; path.MarkDirty(); }

            ImGui.TableNextColumn();
            float z = p.Z;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat("##z", ref z, 0, 0, "%.3f")) { p.Z = z; path.Points[i] = p; path.MarkDirty(); }

            if (hasOrientation)
            {
                ImGui.TableNextColumn();
                float o = p.Orientation ?? 0f;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputFloat("##o", ref o, 0, 0, "%.3f")) { p.Orientation = o; path.Points[i] = p; path.MarkDirty(); }
            }

            ImGui.TableNextColumn();
            if (ImGui.SmallButton("x"))
                removeAt = i;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove this waypoint (applied on Save)");

            ImGui.PopID();
        }

        ImGui.EndTable();

        if (removeAt >= 0)
            RemovePoint(path, removeAt);
    }

    private delegate void SetU(ref UniversalWaypoint w, uint v);
    private delegate void SetF(ref UniversalWaypoint w, float v);
    private delegate void SetI(ref UniversalWaypoint w, int v);
    private delegate void SetB(ref UniversalWaypoint w, bool v);

    private static bool Bool(string label, ref UniversalWaypoint p, SetB set, bool value)
    {
        bool v = value;
        if (ImGui.Checkbox(label, ref v))
        {
            set(ref p, v);
            return true;
        }
        return false;
    }

    private static bool UInt(string label, ref UniversalWaypoint p, SetU set, uint value)
    {
        int v = (int)value;
        if (ImGui.InputInt(label, ref v))
        {
            set(ref p, (uint)Math.Max(0, v));
            return true;
        }
        return false;
    }

    private static bool IntField(string label, ref UniversalWaypoint p, SetI set, int value)
    {
        int v = value;
        if (ImGui.InputInt(label, ref v))
        {
            set(ref p, v);
            return true;
        }
        return false;
    }

    private static bool FloatNullable(string label, ref UniversalWaypoint p, SetF set, float? value, string format = "%.3f")
    {
        float v = value ?? 0f;
        if (ImGui.InputFloat(label, ref v, 0, 0, format))
        {
            set(ref p, v);
            return true;
        }
        return false;
    }

    private static bool Comment(ref UniversalWaypoint p)
    {
        string c = p.Comment ?? "";
        if (ImGui.InputText("Comment", ref c, 256))
        {
            p.Comment = c;
            return true;
        }
        return false;
    }

    private void StartEnumerate()
    {
        if (loadSource.UsesSecondaryKey())
        {
            loadCompoundTask = service.EnumerateCompoundKeys(loadSource);
            loadIdsTask = null;
        }
        else
        {
            loadIdsTask = service.EnumeratePathIds(loadSource);
            loadCompoundTask = null;
        }
    }

    private void DrawLoadPopup()
    {
        if (openLoadPopup)
        {
            ImGui.OpenPopup("Load waypoint path");
            openLoadPopup = false;
        }

        var viewport = ImGui.GetMainViewport();
        var center = viewport.Pos + viewport.Size * 0.5f;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Load waypoint path", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        if (ImGui.BeginCombo("Source", loadSource.ToName()))
        {
            foreach (var src in service.AvailableSources)
            {
                if (ImGui.Selectable(src.ToName(), src == loadSource))
                {
                    loadSource = src;
                    StartEnumerate();
                }
            }
            ImGui.EndCombo();
        }

        if (loadSource.UsesSecondaryKey())
            DrawCompoundLoad();
        else
            DrawSingleKeyLoad();

        ImGui.EndPopup();
    }

    // The load UI for single-key sources (path id or creature guid): a filterable id list + a
    // "new path" id entry, guarding against creating over an id that already has rows.
    private void DrawSingleKeyLoad()
    {
        ImGui.SeparatorText("Existing");
        if (loadIdsTask is { IsCompletedSuccessfully: true } t)
        {
            ImGui.SetNextItemWidth(260);
            ImGui.InputTextWithHint("##loadfilter", "Filter ids...", ref loadFilter, 32);
            if (ImGui.BeginListBox("##ids", new Vector2(260, 220)))
            {
                int shown = 0;
                foreach (var id in t.Result)
                {
                    var idText = id.ToString();
                    if (loadFilter.Length > 0 && !idText.Contains(loadFilter, StringComparison.Ordinal))
                        continue;
                    shown++;
                    bool alreadyOpen = service.FindLoaded(loadSource, id) != null;
                    if (ImGui.Selectable(alreadyOpen ? $"{idText}  (open)" : idText))
                    {
                        service.LoadPath(loadSource, id).ListenErrors(notifications, $"Failed to load path {id}");
                        ImGui.CloseCurrentPopup();
                    }
                }
                if (shown == 0)
                    ImGui.TextDisabled(t.Result.Count == 0 ? "No existing paths for this source" : $"No ids contain \"{loadFilter}\"");
                ImGui.EndListBox();
            }
        }
        else
        {
            ImGui.TextDisabled(loadIdsTask == null ? "..." : "Loading ids...");
        }

        ImGui.SeparatorText("New path");
        ImGui.SetNextItemWidth(160);
        ImGui.InputInt(loadSource.IsKeyedByGuid() ? "Creature guid" : "Path id", ref newPathId);

        // creating a "new" path under an id that already has rows would silently collide on save
        // (the save is DELETE-whole-path + INSERT) - guard it and offer the load instead
        bool idTaken = newPathId > 0 && loadIdsTask is { IsCompletedSuccessfully: true } done &&
                       done.Result.Contains((uint)newPathId);
        ImGui.SameLine();
        ImGui.BeginDisabled(idTaken);
        if (ImGui.Button("Create") && newPathId > 0)
        {
            var path = service.CreateNew(loadSource, (uint)newPathId);
            service.SelectedPath = path;
            if (path.Points.Count == 0)
                service.EditingPath = path; // fresh empty path: arm pen mode, it's meant to be drawn
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndDisabled();
        if (idTaken)
        {
            ImGui.TextColored(EditorTheme.Warning, $"Id {newPathId} already exists - saving a new empty path would overwrite it.");
            ImGui.SameLine();
            if (ImGui.SmallButton("Load it instead"))
            {
                service.LoadPath(loadSource, (uint)newPathId).ListenErrors(notifications, $"Failed to load path {newPathId}");
                ImGui.CloseCurrentPopup();
            }
        }
    }

    // The load UI for compound-keyed sources (creature_movement_template): an (entry, pathId) list +
    // two "new path" inputs, guarding against creating over an (entry, pathId) pair that already exists.
    private void DrawCompoundLoad()
    {
        ImGui.SeparatorText("Existing (entry : path)");
        if (loadCompoundTask is { IsCompletedSuccessfully: true } t)
        {
            ImGui.SetNextItemWidth(260);
            ImGui.InputTextWithHint("##loadfilter", "Filter...", ref loadFilter, 32);
            if (ImGui.BeginListBox("##keys", new Vector2(260, 220)))
            {
                int shown = 0;
                foreach (var (key, key2) in t.Result)
                {
                    var text = $"{key} : {key2}";
                    if (loadFilter.Length > 0 && !text.Contains(loadFilter, StringComparison.Ordinal))
                        continue;
                    shown++;
                    bool alreadyOpen = service.FindLoaded(loadSource, key, key2) != null;
                    if (ImGui.Selectable(alreadyOpen ? $"{text}  (open)" : text))
                    {
                        service.LoadPath(loadSource, key, 0, key2).ListenErrors(notifications, $"Failed to load path {text}");
                        ImGui.CloseCurrentPopup();
                    }
                }
                if (shown == 0)
                    ImGui.TextDisabled(t.Result.Count == 0 ? "No existing paths for this source" : $"No entries contain \"{loadFilter}\"");
                ImGui.EndListBox();
            }
        }
        else
        {
            ImGui.TextDisabled(loadCompoundTask == null ? "..." : "Loading...");
        }

        ImGui.SeparatorText("New path");
        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("Entry", ref newPathId);
        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("Path id", ref newPathId2);

        bool keyTaken = newPathId > 0 && loadCompoundTask is { IsCompletedSuccessfully: true } done &&
                        done.Result.Contains(((uint)newPathId, (uint)Math.Max(0, newPathId2)));
        ImGui.BeginDisabled(keyTaken);
        if (ImGui.Button("Create") && newPathId > 0)
        {
            var path = service.CreateNew(loadSource, (uint)newPathId, (uint)Math.Max(0, newPathId2));
            service.SelectedPath = path;
            if (path.Points.Count == 0)
                service.EditingPath = path; // fresh empty path: arm pen mode, it's meant to be drawn
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndDisabled();
        if (keyTaken)
        {
            ImGui.TextColored(EditorTheme.Warning, $"Entry {newPathId} / path {newPathId2} already exists - saving a new empty path would overwrite it.");
            if (ImGui.SmallButton("Load it instead"))
            {
                service.LoadPath(loadSource, (uint)newPathId, 0, (uint)Math.Max(0, newPathId2))
                    .ListenErrors(notifications, $"Failed to load path {newPathId}:{newPathId2}");
                ImGui.CloseCurrentPopup();
            }
        }
    }

    // The full property popover for a single waypoint - every field its source/table supports.
    // Non-modal and anchored where the user double-clicked (the point stays visible while editing);
    // clicking anywhere else dismisses it, edits apply live.
    private void DrawPropertiesPopover()
    {
        if (openPropsPopup)
        {
            ImGui.OpenPopup("##waypoint_props");
            openPropsPopup = false;
        }

        ImGui.SetNextWindowPos(propsAnchor + new Vector2(14, 10), ImGuiCond.Appearing);
        if (!ImGuiEx.BeginPopup("##waypoint_props"))
            return;

        var path = propsPath;
        if (path == null || propsIndex < 0 || propsIndex >= path.Points.Count || !service.LoadedPaths.Contains(path))
        {
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
            return;
        }

        ImGui.TextDisabled($"{path.DisplayName}  ·  point #{propsIndex + 1}");
        ImGui.Separator();

        var p = path.Points[propsIndex];
        bool changed = false;
        var columns = service.ColumnsFor(path.Source);

        var pos = new Vector3(p.X, p.Y, p.Z);
        ImGui.SetNextItemWidth(230);
        if (ImGui.InputFloat3("Position", ref pos, "%.2f"))
        {
            p.X = pos.X;
            p.Y = pos.Y;
            p.Z = pos.Z;
            changed = true;
        }
        if (columns.HasFlagFast(WaypointColumns.Orientation))
        {
            changed |= FloatNullable("Orientation", ref p, (ref UniversalWaypoint w, float v) => w.Orientation = v, p.Orientation, "%.3f rad");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Facing at this point, radians ({(p.Orientation ?? 0f) * 180f / MathF.PI:0.#}°)");
        }

        ImGui.Separator();
        changed |= DrawSourceFields(path.Source, columns, ref p, full: true, path, propsIndex);

        if (changed)
        {
            path.Points[propsIndex] = p;
            path.MarkDirty();
        }

        ImGui.EndPopup();
    }
}
