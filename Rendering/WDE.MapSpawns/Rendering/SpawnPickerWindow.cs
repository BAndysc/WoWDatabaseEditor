using TheEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hexa.NET.ImGui;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// Searchable creature/gameobject template picker for spawning. Publishes a create request via
/// <see cref="Models.IWorldSpawnEditService"/> (a no-op unless the full-editor bridge is present).
/// Rendered from <see cref="SpawnViewer.RenderGUI"/>; toggled from the toolbar.
/// </summary>
// NOT [AutoRegister]: reachable (via SpawnPlacementController) into IGameContext, which lives only in
// the per-game child container. SpawnViewer owns the single instance and wires it up.
public class SpawnPickerWindow
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly Models.IWorldSpawnEditService editService;
    private SpawnPlacementController? placement;

    private readonly struct Template
    {
        public readonly bool IsCreature;
        public readonly uint Entry;
        public readonly string Name;
        public readonly string Search;
        public Template(bool isCreature, uint entry, string name)
        {
            IsCreature = isCreature;
            Entry = entry;
            Name = name;
            Search = $"{entry} {name}".ToLowerInvariant();
        }
    }

    private volatile List<Template>? all;
    private bool loading;
    private readonly List<Template> filtered = new();
    private string search = "";
    private string appliedSearch = "";
    private bool showCreatures = true;
    private bool showGameObjects = true;
    private bool filterDirty = true;
    private int selectedIndex = -1;
    private int previewedIndex = -1;
    private SpawnPreviewRenderStage? preview;

    public bool IsOpen;

    /// <summary>True while the picker window is focused (recorded each frame) - Escape then closes
    /// the picker, and the spawn-level escape chain stands down. Read a frame late by design: the
    /// chain runs in Update, before this frame's RenderGUI.</summary>
    public bool ConsumesEscape { get; private set; }

    private bool justOpened;
    private bool keepOpen; // pinned: placing does not close the picker (alternating entries)
    private readonly List<Template> recents = new(); // most recent first, capped
    private const int MaxRecents = 8;

    // set on the UI thread by the native "Add creature/gameobject" menu, consumed at the top of
    // RenderGUI on the engine thread which owns all the window/filter state
    // (0 = none, 1 = creatures, 2 = gameobjects)
    private volatile int pendingOpenRequest;

    /// <summary>Opens the picker pre-filtered to creatures or gameobjects (the world context menu's
    /// "Add creature/gameobject") and focuses the search box. Callable from any thread.</summary>
    public void OpenFor(bool creatures)
    {
        pendingOpenRequest = creatures ? 1 : 2;
    }

    public void AttachPreview(SpawnPreviewRenderStage stage) => preview = stage;
    public void AttachPlacement(SpawnPlacementController controller) => placement = controller;

    public SpawnPickerWindow(IDatabaseProvider databaseProvider,
        Models.IWorldSpawnEditService editService)
    {
        this.databaseProvider = databaseProvider;
        this.editService = editService;
    }

    public void RenderGUI()
    {
        var openRequest = pendingOpenRequest;
        if (openRequest != 0)
        {
            pendingOpenRequest = 0;
            IsOpen = true;
            justOpened = true;
            showCreatures = openRequest == 1;
            showGameObjects = openRequest == 2;
            filterDirty = true;
        }

        if (!IsOpen)
        {
            ConsumesEscape = false;
            return;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(560, 420), ImGuiCond.FirstUseEver);
        if (justOpened)
            ImGui.SetNextWindowFocus();
        if (!ImGui.Begin("Spawn", ref IsOpen))
        {
            ImGui.End();
            return;
        }

        ConsumesEscape = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        if (ImGuiEx.WindowWantsCloseOnEscape())
            IsOpen = false;

        if (all == null)
        {
            if (!loading)
            {
                loading = true;
                LoadTemplates().ListenErrors();
            }
            ImGui.TextUnformatted("Loading templates...");
            ImGui.End();
            return;
        }

        if (ImGui.Checkbox("Creatures", ref showCreatures)) filterDirty = true;
        ImGui.SameLine();
        if (ImGui.Checkbox("GameObjects", ref showGameObjects)) filterDirty = true;

        if (justOpened)
        {
            ImGui.SetKeyboardFocusHere();
            justOpened = false;
        }
        if (ImGui.InputText("Search", ref search, 256) || !string.Equals(search, appliedSearch, StringComparison.Ordinal))
        {
            appliedSearch = search;
            filterDirty = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Matches entry and name; words match independently\n(\"looter defias\" finds \"Defias Looter\")");

        DrawRecents();

        if (filterDirty)
        {
            ApplyFilter();
            filterDirty = false;
        }

        ImGui.TextDisabled($"{filtered.Count} templates");

        var avail = ImGui.GetContentRegionAvail();
        float bodyHeight = MathF.Max(avail.Y - ImGui.GetFrameHeightWithSpacing(), 0);
        const float previewWidth = 168f;
        float listWidth = avail.X > previewWidth + 16f ? avail.X - previewWidth - 8f : avail.X;

        // left: the template list
        int hovered = -1;
        ImGui.BeginChild("list", new System.Numerics.Vector2(listWidth, bodyHeight));
        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var clipper = new ImGuiListClipper();
        clipper.Begin(filtered.Count, lineHeight);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                if (i >= filtered.Count) break;
                var t = filtered[i];
                bool selected = i == selectedIndex;
                if (ImGui.Selectable($"{(t.IsCreature ? "C" : "G")} {t.Entry}  {t.Name}##{i}", selected))
                    selectedIndex = i;
                if (ImGui.IsItemHovered())
                {
                    hovered = i;
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        selectedIndex = i;
                        BeginPlacement();
                    }
                }
            }
        }
        clipper.End();
        ImGui.EndChild();

        // the selected row owns the preview; hovering only previews while nothing is selected
        // (otherwise a stray mouse move replaces the model the user just picked)
        int previewTarget = selectedIndex >= 0 && selectedIndex < filtered.Count ? selectedIndex
            : hovered >= 0 ? hovered
            : previewedIndex;
        if (previewTarget >= 0 && previewTarget != previewedIndex && previewTarget < filtered.Count)
        {
            previewedIndex = previewTarget;
            var sel = filtered[previewTarget];
            preview?.SetTemplate(sel.IsCreature, sel.Entry);
        }

        // right: the rendered 3D preview + selected template name
        ImGui.SameLine();
        ImGui.BeginChild("preview", new System.Numerics.Vector2(previewWidth, bodyHeight));
        if (preview != null && preview.HasPreview)
        {
            unsafe
            {
                ImGui.Image(new ImTextureRef(null, preview.PreviewTextureId), new System.Numerics.Vector2(previewWidth - 8, previewWidth - 8));
            }
        }
        else if (previewedIndex >= 0)
        {
            ImGui.TextDisabled("rendering preview...");
        }
        if (previewedIndex >= 0 && previewedIndex < filtered.Count)
            ImGui.TextWrapped(filtered[previewedIndex].Name);
        ImGui.EndChild();

        ImGui.BeginDisabled(!editService.IsAvailable || selectedIndex < 0 || selectedIndex >= filtered.Count);
        if (ImGui.Button("Place in world"))
            BeginPlacement();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.Checkbox("Pin", ref keepOpen);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Keep the picker open after placing -\nhandy when alternating between entries");
        if (!editService.IsAvailable)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(spawning available in the full editor)");
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextDisabled("click to place, Shift for multiple");
        }

        ImGui.End();
    }

    // pick the template, close the window (unless pinned), hand off to the phantom placement controller
    private void BeginPlacement()
    {
        if (selectedIndex < 0 || selectedIndex >= filtered.Count)
            return;
        PlaceTemplate(filtered[selectedIndex]);
    }

    private void PlaceTemplate(Template t)
    {
        if (placement == null)
            return;
        recents.RemoveAll(r => r.IsCreature == t.IsCreature && r.Entry == t.Entry);
        recents.Insert(0, t);
        if (recents.Count > MaxRecents)
            recents.RemoveAt(recents.Count - 1);
        placement.Begin(t.IsCreature, t.Entry);
        if (!keepOpen)
            IsOpen = false;
    }

    // one-click re-placement of recently placed templates, wrapped to the window width
    private void DrawRecents()
    {
        if (recents.Count == 0 || !editService.IsAvailable)
            return;

        ImGui.TextDisabled("Recent:");
        float right = ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X;
        for (int i = 0; i < recents.Count; ++i)
        {
            var r = recents[i];
            string name = r.Name.Length > 24 ? r.Name[..24] + "…" : r.Name;
            string label = $"{name}##recent{i}";
            float width = ImGui.CalcTextSize(name).X + ImGui.GetStyle().FramePadding.X * 2 + 4;
            ImGui.SameLine();
            if (ImGui.GetCursorPosX() + width > right)
                ImGui.NewLine();
            if (ImGui.SmallButton(label))
                PlaceTemplate(r);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Place {(r.IsCreature ? "creature" : "gameobject")} {r.Entry} {r.Name}");
        }
    }

    private void ApplyFilter()
    {
        filtered.Clear();
        selectedIndex = -1;
        if (all == null)
            return;
        // whitespace-separated tokens match independently ("looter defias" finds "Defias Looter");
        // trimmed, so a trailing space never kills every match
        var tokens = appliedSearch.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var t in all)
        {
            if (t.IsCreature && !showCreatures) continue;
            if (!t.IsCreature && !showGameObjects) continue;
            bool match = true;
            foreach (var token in tokens)
            {
                if (!t.Search.Contains(token, StringComparison.Ordinal))
                {
                    match = false;
                    break;
                }
            }
            if (match)
                filtered.Add(t);
        }
    }

    private async Task LoadTemplates()
    {
        var creatures = await databaseProvider.GetCreatureTemplatesAsync();
        var gameobjects = await databaseProvider.GetGameObjectTemplatesAsync();
        var list = new List<Template>(creatures.Count + gameobjects.Count);
        foreach (var c in creatures)
            list.Add(new Template(true, c.Entry, c.Name ?? ""));
        foreach (var g in gameobjects)
            list.Add(new Template(false, g.Entry, g.Name ?? ""));
        all = list;
        filterDirty = true;
    }
}
