using TheEngine;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Formations;

namespace WDE.MapSpawns.Rendering.Formations;

/// <summary>
/// The Formation tool's inspector section: the selected leader↔member link edited inline
/// (dist/angle/groupAI/point_1/point_2 - the world arrow updates live) plus a compact links list
/// in an expander. Also the per-link properties popover (non-modal, anchored where the user
/// double-clicked). Pure UI - state lives in <see cref="IFormationEditorService"/>; world
/// interaction (drag/pick) lives in <see cref="FormationEditorModule"/>.
/// </summary>
public sealed class FormationInspector : IInspectorSection
{
    private readonly IFormationEditorService service;
    private readonly IGameContext gameContext;

    private bool openPropsPopup;
    private Vector2 propsAnchor;
    private EditableFormation? propsFormation;

    // reused each frame; holds the filtered rows shown in the list (selected spawn's group + dirty)
    private readonly List<EditableFormation> listed = new();

    public FormationInspector(IFormationEditorService service, IGameContext gameContext)
    {
        this.service = service;
        this.gameContext = gameContext;
    }

    // "Creature formations" - the spawn-group editor has its own "Group formation" section
    // (a different table and system); the two must not share one name
    public string Title => "Creature formations";

    public bool IsDirty => service.IsSupported && service.AnyDirty;

    public Func<Task>? SaveSelf => IsDirty ? service.Save : null;

    public Func<Task>? RevertSelf => IsDirty ? (() => service.LoadForMap(service.LoadedMap)) : null;

    public string? Hints
    {
        get
        {
            if (!service.IsSupported)
                return "The current core has no creature_formations table";
            if (service.Selected != null)
                return "Del: remove link · double-click arrow: properties · drag a creature onto a leader: new link";
            return "Drag a creature onto its leader to link · click an arrow to select";
        }
    }

    /// <summary>Opens the link property popover, anchored at the current mouse position (i.e. at
    /// the double-clicked list row or world arrow).</summary>
    public void OpenProperties(EditableFormation formation)
    {
        propsFormation = formation;
        propsAnchor = ImGui.GetMousePos();
        openPropsPopup = true;
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\ncreature_formations table."u8);
            return;
        }

        if (service.Selected is { } selected)
        {
            if (EditorWidgets.BackRow("Deselect link", "Stop editing this link (it stays in the world)"))
            {
                service.Selected = null;
                return;
            }
            ImGui.TextUnformatted($"member {selected.MemberGuid}  {Lucide.ArrowRight}  leader {selected.LeaderGuid}");
            if (DrawParams(selected))
                return; // removed
        }
        else
        {
            ImGui.TextDisabled("No link selected."u8);
            ImGui.TextDisabled("Create a link by dragging a creature onto\nits leader in the world; click an arrow\nto edit an existing one."u8);
        }

        DrawManualAdd();

        service.CollectListedFormations(listed);
        if (ImGui.CollapsingHeader($"Links ({listed.Count} shown, {service.LoadedFormations.Count} in world)###links"))
            DrawList();
    }

    private int addMemberGuid;
    private int addLeaderGuid;
    private string? addError;

    // typing the guids covers pairs the drag gesture can't reach (distant, occluded, inside buildings)
    private void DrawManualAdd()
    {
        ImGui.SeparatorText("New link by guid"u8);
        ImGui.SetNextItemWidth(90);
        ImGui.InputInt("##addmember"u8, ref addMemberGuid, 0, 0);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Member creature guid"u8);
        ImGui.SameLine();
        ImGui.TextDisabled(Lucide.ArrowRight);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90);
        ImGui.InputInt("##addleader"u8, ref addLeaderGuid, 0, 0);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Leader creature guid"u8);
        ImGui.SameLine();
        ImGui.BeginDisabled(addMemberGuid <= 0 || addLeaderGuid <= 0 || addMemberGuid == addLeaderGuid);
        if (ImGui.SmallButton($"{Lucide.Link} Link"))
        {
            // Add computes dist/angle from the live positions and re-links an already-linked member
            var created = service.Add((uint)addMemberGuid, (uint)addLeaderGuid);
            addError = created == null ? "Both guids must be creature spawns loaded on this map" : null;
            if (created != null)
            {
                addMemberGuid = 0;
                addLeaderGuid = 0;
            }
        }
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(addMemberGuid == addLeaderGuid && addMemberGuid > 0
                ? "A creature can't lead itself"u8
                : "Creates the member -> leader link (applied on Save)"u8);
        if (addError != null)
            EditorWidgets.WrappedWarning(addError);
    }

    /// <summary>Drawn unconditionally from the module's RenderGUI, so a world double-click opens
    /// the popover no matter the panel state.</summary>
    public void DrawPopups() => DrawPropertiesPopover();

    /// <summary>The editable link fields; live - the world arrow follows. True when the link was removed.</summary>
    private bool DrawParams(EditableFormation f)
    {
        float dist = f.Dist;
        float angle = f.Angle;
        int groupAi = (int)f.GroupAi;
        int point1 = (int)f.Point1;
        int point2 = (int)f.Point2;
        bool changed = false;

        // drag-scrub: the world arrow follows live, so scrubbing while watching it is the natural
        // way to tune these (Ctrl+click still types an exact value)
        EditorWidgets.FitNextItem("Dist"u8);
        changed |= ImGui.DragFloat("Dist"u8, ref dist, 0.05f, 0f, 100f, "%.3f"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Follow distance from the leader.\nDrag to scrub while watching the arrow; Ctrl+click to type."u8);
        EditorWidgets.FitNextItem("Angle (deg)"u8);
        changed |= ImGui.DragFloat("Angle (deg)"u8, ref angle, 0.5f, -360f, 360f, "%.3f"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Follow angle relative to the leader's facing ({angle * MathF.PI / 180f:0.###} rad).\nThe formation table stores this column in degrees - unlike\norientations, which are radians everywhere else.\nDrag to scrub while watching the arrow; Ctrl+click to type.");
        EditorWidgets.FitNextItem("groupAI"u8);
        changed |= ImGui.InputInt("groupAI"u8, ref groupAi);
        EditorWidgets.FitNextItem("point_1"u8);
        changed |= ImGui.InputInt("point_1"u8, ref point1);
        EditorWidgets.FitNextItem("point_2"u8);
        changed |= ImGui.InputInt("point_2"u8, ref point2);

        if (changed)
            f.SetParams(dist, angle, (uint)Math.Max(0, groupAi), (uint)Math.Max(0, point1), (uint)Math.Max(0, point2));

        // destructive-styled like the other editors' deletes; it's a pending change, not a DB write
        EditorTheme.PushDestructiveButton();
        bool remove = ImGui.SmallButton($"{Lucide.Trash2} Remove link");
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Unlink this creature from its leader.\nPending until Save - Revert restores it."u8);
        if (remove)
        {
            service.Remove(f);
            return true;
        }
        return false;
    }

    private void DrawList()
    {
        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
                         ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("formations"u8, 6, tableFlags, new Vector2(0, 160)))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Member"u8);
        ImGui.TableSetupColumn("Leader"u8);
        ImGui.TableSetupColumn("Dist"u8);
        ImGui.TableSetupColumn("Angle"u8);
        ImGui.TableSetupColumn(""u8, ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableSetupColumn(""u8, ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableHeadersRow();

        EditableFormation? removeTarget = null;

        // 'listed' was filled in DrawContent (selected spawn's group + dirty groups, self-rows excluded)
        for (int i = 0; i < listed.Count; ++i)
        {
            var f = listed[i];

            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableNextColumn();
            bool selected = ReferenceEquals(f, service.Selected);
            ImGui.SetNextItemAllowOverlap();
            if (ImGui.Selectable(f.MemberGuid.ToString() + (f.IsDirty ? " *" : ""), selected, ImGuiSelectableFlags.SpanAllColumns))
                service.Selected = f;
            if (ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    service.Selected = f;
                    FlyTo(f);
                }
                else if (ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip))
                    ImGui.SetTooltip("Click: select · double-click: fly camera to it"u8);
            }

            ImGui.TableNextColumn();
            ImGui.Text(f.LeaderGuid.ToString());
            ImGui.TableNextColumn();
            ImGui.Text(f.Dist.ToString("0.##"));
            ImGui.TableNextColumn();
            ImGui.Text(f.Angle.ToString("0.#"));

            ImGui.TableNextColumn();
            if (ImGui.SmallButton(Lucide.Ellipsis))
            {
                service.Selected = f;
                OpenProperties(f);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("All link properties"u8);

            ImGui.TableNextColumn();
            if (ImGui.SmallButton(Lucide.X))
                removeTarget = f;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove this member-leader formation link (applied on Save)"u8);

            ImGui.PopID();
        }

        ImGui.EndTable();

        if (removeTarget != null)
            service.Remove(removeTarget);
    }

    private void FlyTo(EditableFormation f)
    {
        if (service.TryGetEndpoints(f, out _, out var memberPos))
            gameContext.CameraManager.Relocate(memberPos, flyHere: true);
    }

    // Non-modal, anchored at the double-click position so the arrow stays visible while editing;
    // clicking anywhere else dismisses it, edits apply live.
    private void DrawPropertiesPopover()
    {
        if (openPropsPopup)
        {
            ImGui.OpenPopup("##formation_props"u8);
            openPropsPopup = false;
        }

        ImGui.SetNextWindowPos(propsAnchor + new Vector2(14, 10), ImGuiCond.Appearing);
        if (!ImGuiEx.BeginPopup("##formation_props"))
            return;

        var f = propsFormation;
        if (f == null || !service.LoadedFormations.Contains(f))
        {
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
            return;
        }

        ImGui.TextDisabled($"member {f.MemberGuid}  {Lucide.ArrowRight}  leader {f.LeaderGuid}");
        ImGui.Separator();
        if (DrawParams(f))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }
}
