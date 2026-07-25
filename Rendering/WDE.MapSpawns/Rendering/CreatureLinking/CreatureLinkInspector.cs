using System;
using System.Numerics;
using System.Threading.Tasks;
using Hexa.NET.ImGui;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.CreatureLinking;

namespace WDE.MapSpawns.Rendering.CreatureLinking;

/// <summary>
/// The Creature-linking tool's inspector: pick the link mode (guid vs entry) and edit the selected
/// link's flags (+ search range for entry links). Links themselves are created/selected in the
/// world (drag one creature onto another; click an arrow). World interaction lives in
/// <see cref="CreatureLinkEditorModule"/>; state in <see cref="ICreatureLinkEditorService"/>.
/// </summary>
public sealed class CreatureLinkInspector : IInspectorSection
{
    private readonly ICreatureLinkEditorService service;

    public CreatureLinkInspector(ICreatureLinkEditorService service)
    {
        this.service = service;
    }

    public string Title => "Creature linking";

    public bool IsDirty => service.IsSupported && service.AnyDirty;

    public Func<Task>? SaveSelf => IsDirty ? service.Save : null;

    public Func<Task>? RevertSelf => IsDirty ? (() => service.LoadForMap(service.LoadedMap)) : null;

    public string? Hints
    {
        get
        {
            if (!service.IsSupported)
                return "The current core has no creature_linking table";
            if (service.Selected != null)
                return "Del: remove link · drag a creature onto another: link it as slave → master";
            return service.Mode == CreatureLinkMode.Entry
                ? "By entry: drag a creature onto another to link their entries · click an arrow to select"
                : "By guid: drag a creature onto another to link them · click an arrow to select";
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\ncreature_linking table.");
            return;
        }

        DrawModePicker();
        ImGui.Separator();

        switch (service.Selected)
        {
            case EditableCreatureLink guidLink:
                DrawGuidLinkEditor(guidLink);
                break;
            case EditableCreatureLinkTemplate templateLink:
                DrawTemplateLinkEditor(templateLink);
                break;
            default:
                ImGui.TextDisabled("No link selected.");
                ImGui.TextDisabled("Drag a creature onto another to link it\n(slave → master); click an arrow to edit.");
                break;
        }

        DecalLegend.Draw(
            (CreatureLinkRenderStage.GuidArrowColor, "guid link"),
            (CreatureLinkRenderStage.EntryArrowColor, "entry link"),
            (CreatureLinkRenderStage.SelectedColor, "selected"));
    }

    private void DrawModePicker()
    {
        ImGui.TextUnformatted("New link creates:");
        if (ImGui.RadioButton("creature_linking (guid)", service.Mode == CreatureLinkMode.Guid))
            service.Mode = CreatureLinkMode.Guid;
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Links one concrete slave spawn to one master spawn (by guid)");

        ImGui.BeginDisabled(!service.SupportsTemplateLinks);
        if (ImGui.RadioButton("creature_linking_template (entry)", service.Mode == CreatureLinkMode.Entry))
            service.Mode = CreatureLinkMode.Entry;
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(service.SupportsTemplateLinks
                ? "Links every spawn of the slave entry on this map to the master entry"
                : "The current core has no creature_linking_template table");
    }

    // ------------------------------------------------------------------ selected editors ---------

    private void DrawGuidLinkEditor(EditableCreatureLink link)
    {
        ImGui.TextUnformatted($"slave {link.Guid}  →  master {link.MasterGuid}");
        DrawFlags(link.Flag, link.SetFlag);
        DrawRemoveButton(() => service.RemoveGuidLink(link));
    }

    private void DrawTemplateLinkEditor(EditableCreatureLinkTemplate link)
    {
        ImGui.TextUnformatted($"entry {link.Entry} (map {link.Map})  →  master entry {link.MasterEntry}");

        int range = (int)link.SearchRange;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("Search range", ref range))
            link.SetSearchRange((uint)Math.Max(0, range));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Range (from spawn coordinates) master and slave are paired within.\n0 = whole map (the master entry must have exactly one spawn).");

        DrawFlags(link.Flag, link.SetFlag);
        DrawRemoveButton(() => service.RemoveTemplateLink(link));
    }

    private void DrawFlags(uint flag, Action<uint> setFlag)
    {
        int setCount = System.Numerics.BitOperations.PopCount(flag);
        if (!ImGui.CollapsingHeader($"Flags ({setCount} set)###linkflags"))
            return;

        uint newFlag = flag;
        foreach (var (bit, name) in CreatureLinkFlags.All)
        {
            bool on = (flag & bit) != 0;
            if (ImGui.Checkbox($"{name}##flag{bit}", ref on))
            {
                if (on)
                    newFlag |= bit;
                else
                    newFlag &= ~bit;
            }
        }
        if (newFlag != flag)
            setFlag(newFlag);
    }

    private static void DrawRemoveButton(Action remove)
    {
        EditorTheme.PushDestructiveButton();
        bool clicked = ImGui.SmallButton("Remove link");
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Remove this link.\nPending until Save - Revert restores it.");
        if (clicked)
            remove();
    }
}
