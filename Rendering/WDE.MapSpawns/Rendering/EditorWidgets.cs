using System;
using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using TheEngine;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// Shared inspector widgets that keep the panels visually consistent.
/// </summary>
public static class EditorWidgets
{
    // per-combo filter text, keyed by the combo's ImGui label; reset every time its popup opens
    private static readonly Dictionary<string, string> comboFilters = new();

    /// <summary>How many items an <see cref="IdNameCombo"/> needs before it grows a filter box.</summary>
    private const int FilterThreshold = 8;

    /// <summary>
    /// A "pick an id+name row" combo whose POPUP is clamped to the combo's own width - by default
    /// one long item label blows an ImGui combo popup up to screen width, detached from the panel.
    /// Long labels clip and show their full text as a tooltip instead. Lists longer than a few
    /// items get a filter box (matches id and name). Set the item width before calling, as for any
    /// combo. Returns true and sets <paramref name="picked"/> when an item was clicked.
    /// </summary>
    public static bool IdNameCombo(string label, string preview, IEnumerable<(uint id, string name)> items,
        uint selected, out uint picked, int itemCount = -1)
    {
        picked = 0;
        float width = MathF.Max(ImGui.CalcItemWidth(), 120f);
        // popup: exactly the combo's width, at most ~13 rows tall (then it scrolls)
        ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0), new Vector2(width, 320));
        if (!ImGui.BeginCombo(label, preview, ImGuiComboFlags.HeightLargest))
            return false;

        bool changed = false;
        comboFilters.TryGetValue(label, out var filter);
        filter ??= "";
        if (ImGui.IsWindowAppearing())
            filter = "";

        bool useFilter = itemCount is < 0 or >= FilterThreshold;
        if (useFilter)
        {
            if (ImGui.IsWindowAppearing())
                ImGui.SetKeyboardFocusHere();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint($"##{label}filter", "filter...", ref filter, 128);
        }
        comboFilters[label] = filter;

        float availX = ImGui.GetContentRegionAvail().X;
        foreach (var (id, name) in items)
        {
            var text = $"{id} {name}";
            if (filter.Length > 0 && !text.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            if (ImGui.Selectable(text, id == selected))
            {
                picked = id;
                changed = true;
            }
            // a clipped label's full text rides a tooltip
            if (ImGui.IsItemHovered() && ImGui.CalcTextSize(text).X > availX)
                ImGui.SetTooltip(text);
        }
        ImGui.EndCombo();
        return changed;
    }

    // per-popup filter text, keyed by the modal title; reset every time the popup opens
    private static readonly Dictionary<string, string> popupFilters = new();

    /// <summary>
    /// The "load an existing id+name row" flow: one button that opens a centered modal with a
    /// filter box + list - instead of a permanently visible combo. Returns true and sets
    /// <paramref name="picked"/> exactly once, when a row is clicked.
    /// </summary>
    public static bool LoadExistingPopup(string buttonLabel, string modalTitle,
        IEnumerable<(uint id, string name)> items, out uint picked)
    {
        picked = 0;
        if (ImGui.SmallButton(buttonLabel))
            ImGui.OpenPopup(modalTitle);

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + viewport.Size * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        bool open = true;
        if (!ImGuiEx.BeginPopupModal(modalTitle, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return false;

        popupFilters.TryGetValue(modalTitle, out var filter);
        filter ??= "";
        if (ImGui.IsWindowAppearing())
        {
            filter = "";
            ImGui.SetKeyboardFocusHere();
        }
        ImGui.SetNextItemWidth(340);
        ImGui.InputTextWithHint("##loadfilter", "filter by id or name...", ref filter, 128);
        popupFilters[modalTitle] = filter;

        bool pickedAny = false;
        if (ImGui.BeginListBox("##loadrows", new Vector2(340, 260)))
        {
            float availX = ImGui.GetContentRegionAvail().X;
            int shown = 0, total = 0;
            foreach (var (id, name) in items)
            {
                total++;
                var label = $"{id} {name}";
                if (filter.Length > 0 && !label.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
                shown++;
                if (ImGui.Selectable(label))
                {
                    picked = id;
                    pickedAny = true;
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.IsItemHovered() && ImGui.CalcTextSize(label).X > availX)
                    ImGui.SetTooltip(label);
            }
            if (total == 0)
                ImGui.TextDisabled("Nothing to load.");
            else if (shown == 0)
                ImGui.TextDisabled($"Nothing matches \"{filter}\"");
            ImGui.EndListBox();
        }
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
        return pickedAny;
    }

    /// <summary>
    /// The "create a named thing" flow: one small button that opens a modal asking for the name -
    /// instead of a permanently visible name textbox + Create pair. Returns the typed name exactly
    /// once, on confirm (null otherwise). The modal id doubles as its title.
    /// </summary>
    public static string? CreateNamePopup(string buttonLabel, string modalTitle, string nameHint,
        ref string nameBuffer, bool buttonEnabled = true, string? disabledTooltip = null, bool fullWidthButton = false)
    {
        ImGui.BeginDisabled(!buttonEnabled);
        bool clicked = fullWidthButton
            ? ImGui.Button(buttonLabel, new Vector2(ImGui.GetContentRegionAvail().X, 0))
            : ImGui.Button(buttonLabel);
        ImGui.EndDisabled();
        if (!buttonEnabled && disabledTooltip != null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(disabledTooltip);
        if (clicked)
        {
            nameBuffer = "";
            ImGui.OpenPopup(modalTitle);
        }

        bool open = true;
        if (!ImGuiEx.BeginPopupModal(modalTitle, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return null;

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(280);
        bool submitted = ImGui.InputTextWithHint("##createname", nameHint, ref nameBuffer, 128,
            ImGuiInputTextFlags.EnterReturnsTrue);

        string? result = null;
        bool nameEmpty = string.IsNullOrWhiteSpace(nameBuffer);
        ImGui.BeginDisabled(nameEmpty);
        if (ImGui.Button("Create", new Vector2(120, 0)) || (submitted && !nameEmpty))
        {
            result = nameBuffer;
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
        return result;
    }
}
