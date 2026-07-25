using TheEngine;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.Common.Database;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.WorldPoints;

namespace WDE.MapSpawns.Rendering.WorldPoints;

/// <summary>
/// The Graveyards tool's inspector: pick/select a safe loc (or arm placement to create one),
/// edit its name/position/orientation and its game_graveyard_zone links (area/map kind, team),
/// with quick "link the area/zone/map under the graveyard" buttons and a delete confirm.
/// </summary>
public sealed class SafeLocInspector : IInspectorSection
{
    private readonly ISafeLocEditorService service;
    private readonly SafeLocEditorModule module;
    private readonly IGameContext gameContext;

    private int newLinkKind;
    private int newLinkLoc;
    private int newLinkFaction;

    private static readonly uint[] FactionValues = { 0, 469, 67 };
    private static readonly string[] FactionNames = { "Both", "Alliance", "Horde" };
    private static readonly string[] KindNames = { "Area/Zone", "Map" };

    public SafeLocInspector(ISafeLocEditorService service, SafeLocEditorModule module, IGameContext gameContext)
    {
        this.service = service;
        this.module = module;
        this.gameContext = gameContext;
    }

    public string Title => "Graveyards";

    public bool IsDirty => service.IsSupported && service.AnyDirty;

    public Func<Task>? SaveSelf => IsDirty ? service.Save : null;

    public Func<Task>? RevertSelf => IsDirty ? RevertAll : null;

    private Task RevertAll()
    {
        module.Reload();
        return Task.CompletedTask;
    }

    public string? Hints
    {
        get
        {
            if (!service.IsSupported)
                return "The current core has no world_safe_locs table";
            if (module.DragHint is { } dragHint)
                return dragHint;
            if (module.PlacementArmed)
                return "Click the world to place the graveyard · pick a marker: edit it";
            if (module.SelectedKey != null)
                return "G grab · G again snap to ground · R rotate (spirit healer facing) · Del: delete · Esc cancel · click empty ground: deselect";
            return "Click a marker to edit it · \"Place graveyard\" then click the world to create one";
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\nworld_safe_locs table."u8);
            return;
        }

        // one selection mechanism: the filtered list below IS the picker (no duplicate combo);
        // an open editor gets a back row instead
        if (module.SelectedKey is { } key && service.Locs.TryGetValue(key, out var loc))
        {
            bool back = EditorWidgets.BackRow("All graveyards",
                "Back to the graveyard list (a marker click reopens the editor)");
            ImGui.Separator();
            if (back)
            {
                module.SelectedKey = null;
                DrawOverview();
            }
            else
                DrawEditor(loc);
        }
        else
            DrawOverview();
    }

    private string overviewFilter = "";

    private void DrawOverview()
    {
        if (module.PlacementArmed)
        {
            if (ImGui.Button($"{Lucide.X} Cancel placement", new Vector2(-1, 0)))
                module.PlacementArmed = false;
            ImGui.TextDisabled("Click the world to place the new graveyard."u8);
        }
        else if (ImGui.Button($"{Lucide.Plus} Place graveyard (click world)", new Vector2(-1, 0)))
            module.PlacementArmed = true;

        ImGui.SeparatorText("Graveyards on this map"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Graveyards on other maps: open their map first"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##gyfilter"u8, "filter by name or id"u8, ref overviewFilter, 100);

        if (ImGui.BeginChild("##gyrows"u8))
        {
            int total = 0, shown = 0;
            foreach (var loc in service.Locs.Values.Where(l => l.Map == (uint)module.CurrentMapId).OrderBy(l => l.Id))
            {
                total++;
                string label = $"{loc.Name} #{loc.Id}";
                if (overviewFilter.Length > 0 && !label.Contains(overviewFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                shown++;
                if (ImGui.Selectable(label))
                    module.SelectedKey = loc.Id;
                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        module.FlyTo(loc.Map, loc.Position);
                    ImGui.SetTooltip("Click: edit · double-click: fly camera to it"u8);
                }
            }
            if (total == 0)
                ImGui.TextDisabled("No graveyards on this map."u8);
            else if (shown == 0)
                ImGui.TextDisabled($"No graveyards match \"{overviewFilter}\"");
        }
        ImGui.EndChild();
    }

    // ---------------------------------------------------------------- editor ---------------------

    private void DrawEditor(SafeLocData loc)
    {
        bool changed = false;
        var dbc = gameContext.DbcManager;

        string name = loc.Name;
        EditorWidgets.FitNextItem("Name"u8);
        if (ImGui.InputText("Name"u8, ref name, 50))
        {
            loc.Name = name;
            changed = true;
        }

        ImGui.TextDisabled($"Id {loc.Id} · {WorldPointNames.MapName(dbc, (int)loc.Map)}");

        var pos = new Vector3(loc.Position.X, loc.Position.Y, loc.Position.Z);
        EditorWidgets.FitNextItem("Position"u8);
        if (ImGui.InputFloat3("Position", ref pos))
        {
            loc.Position = new Vector3(pos.X, pos.Y, pos.Z);
            changed = true;
        }

        float orientation = loc.Orientation;
        EditorWidgets.FitNextItem("Facing"u8);
        if (ImGui.SliderFloat("Facing"u8, ref orientation, 0f, MathF.Tau, "%.3f rad"u8))
        {
            loc.Orientation = orientation;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"The spirit healer faces this way ({orientation * 180f / MathF.PI:0.#}°)\nCtrl+click to type an exact value");

        if (ImGui.SmallButton($"{Lucide.ArrowDownToLine} Snap to ground"))
        {
            loc.Position = module.SnapToGround(loc.Position);
            changed = true;
        }
        ImGui.SameLine();
        if (EditorWidgets.FlyToButton("graveyard"))
            module.FlyTo(loc.Map, loc.Position);

        DrawLinks(loc, ref changed);

        ImGui.Separator();
        DrawDelete(loc);

        if (changed)
            service.NotifyChanged(loc.Id);
    }

    private void DrawLinks(SafeLocData loc, ref bool changed)
    {
        ImGui.SeparatorText("Death links"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Players dying in a linked area/zone (or anywhere on a linked map)\nresurrect here. An unlinked graveyard is never chosen on death."u8);

        var dbc = gameContext.DbcManager;

        int removeAt = -1;
        for (int i = 0; i < loc.Links.Count; ++i)
        {
            var link = loc.Links[i];
            ImGui.PushID(i);

            string where = link.Kind == GraveyardLinkKind.Map
                ? WorldPointNames.MapName(dbc, (int)link.GhostLoc)
                : WorldPointNames.AreaName(dbc, link.GhostLoc);
            string kind = link.Kind == GraveyardLinkKind.Map ? "map" : "area";
            ImGui.TextUnformatted($"{where} ({kind} {link.GhostLoc})");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(90);
            int faction = Array.IndexOf(FactionValues, link.Faction);
            if (faction < 0)
                faction = 0;
            if (ImGui.Combo("##faction", ref faction, FactionNames, FactionNames.Length))
            {
                link.Faction = FactionValues[faction];
                loc.Links[i] = link;
                changed = true;
            }

            if (EditorWidgets.TrailingRemoveButton("Remove the death link - players from this area stop\nresurrecting here (applied on Save)"))
                removeAt = i;

            ImGui.PopID();
        }

        if (removeAt >= 0)
        {
            loc.Links.RemoveAt(removeAt);
            changed = true;
        }

        if (loc.Links.Count == 0)
            ImGui.TextColored(EditorTheme.Warning, "Not linked: never chosen on death"u8);

        // quick links for the location under the graveyard itself
        var (zone, area) = WorldPointNames.ZoneAndArea(dbc, gameContext.ZoneAreaManager, (int)loc.Map, loc.Position);
        if (area.HasValue && ImGui.SmallButton($"Link {WorldPointNames.AreaName(dbc, (uint)area.Value)}"))
            AddLink(loc, (uint)area.Value, GraveyardLinkKind.Area, ref changed);
        if (zone.HasValue && zone != area)
        {
            if (area.HasValue)
                ImGui.SameLine();
            if (ImGui.SmallButton($"Link {WorldPointNames.AreaName(dbc, (uint)zone.Value)}"))
                AddLink(loc, (uint)zone.Value, GraveyardLinkKind.Area, ref changed);
        }
        if (ImGui.SmallButton($"Link whole {WorldPointNames.MapName(dbc, (int)loc.Map)}"))
            AddLink(loc, loc.Map, GraveyardLinkKind.Map, ref changed);

        // manual add row
        ImGui.SetNextItemWidth(86);
        ImGui.Combo("##newkind", ref newLinkKind, KindNames, KindNames.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.InputInt("##newloc"u8, ref newLinkLoc, 0, 0);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(86);
        ImGui.Combo("##newfaction", ref newLinkFaction, FactionNames, FactionNames.Length);
        ImGui.SameLine();
        if (ImGui.SmallButton($"{Lucide.Plus} Add") && newLinkLoc > 0)
        {
            AddLink(loc, (uint)newLinkLoc,
                newLinkKind == 1 ? GraveyardLinkKind.Map : GraveyardLinkKind.Area,
                ref changed, FactionValues[newLinkFaction]);
            newLinkLoc = 0;
        }
    }

    private static void AddLink(SafeLocData loc, uint ghostLoc, GraveyardLinkKind kind, ref bool changed, uint faction = 0)
    {
        if (loc.Links.Any(l => l.GhostLoc == ghostLoc && l.Kind == kind))
            return;
        loc.Links.Add(new GraveyardLinkRow { GhostLoc = ghostLoc, Kind = kind, Faction = faction });
        changed = true;
    }

    private void DrawDelete(SafeLocData loc)
    {
        if (ImGui.Button($"{Lucide.Trash2} Delete graveyard...", new Vector2(-1, 0)))
            ImGui.OpenPopup("Delete graveyard"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Deletes the safe loc and all its death links.\nApplied on Save. Battlegrounds/scripts referencing the id keep the dangling reference!"u8);

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Delete graveyard", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted($"Delete graveyard {loc.Id} \"{loc.Name}\"?");
        ImGui.TextDisabled("The database rows are removed when you Save."u8);
        ImGui.Separator();
        if (ImGui.Button("Delete"u8, new Vector2(120, 0)))
        {
            service.DeleteLoc(loc.Id);
            module.SelectedKey = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8, new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }
}
