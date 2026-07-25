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
    /// <summary>The one back/exit affordance every tool panel uses to leave its selected item:
    /// arrow + where you land ("All pools", "Close path"). Every tool had grown its own spelling
    /// (icon-only, "&lt; back", "X Close") - one widget keeps the reflex portable.</summary>
    public static bool BackRow(string destination, string? tooltip = null)
    {
        bool clicked = ImGui.SmallButton($"{Lucide.ArrowLeft} {destination}");
        if (tooltip != null && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
        return clicked;
    }

    /// <summary>The one fly-the-camera affordance: same icon + verb everywhere, only the target
    /// noun varies ("Fly to trigger", "Fly to graveyard").</summary>
    public static bool FlyToButton(string target)
    {
        return ImGui.SmallButton($"{Lucide.Video} Fly to {target}");
    }

    /// <summary>The trailing remove "X" of a plain (non-table) row: right-aligned so every list -
    /// table or not - deletes on the same edge. Call after the row's content, it places itself.</summary>
    public static bool TrailingRemoveButton(string tooltip)
    {
        // hop onto the row first, then push to the right edge - but never left of where the
        // row's content already ended (narrow panels degrade to inline instead of overlapping)
        ImGui.SameLine();
        float inlineX = ImGui.GetCursorPosX();
        float rightX = ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - 22f;
        ImGui.SameLine(MathF.Max(inlineX, rightX));
        bool clicked = ImGui.SmallButton(Lucide.X);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
        return clicked;
    }

    /// <summary>Sizes the next right-labeled widget (InputInt/InputText/Slider/combo...) so field +
    /// label together fit the panel. ImGui's default item width is 65% of the WINDOW - it ignores
    /// both the label drawn to the right and any tree indent, so default-width rows overflow the
    /// inspector and focusing one auto-scrolls the whole panel sideways. The floor keeps a shrunken
    /// panel from collapsing fields to a sliver (the label clips instead).</summary>
    public static void FitNextItem(ReadOnlySpan<byte> label, float minWidth = 90f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X;
        ImGui.SetNextItemWidth(MathF.Max(minWidth,
            ImGui.GetContentRegionAvail().X - labelWidth - ImGui.GetStyle().ItemInnerSpacing.X));
    }

    /// <inheritdoc cref="FitNextItem(System.ReadOnlySpan{byte},float)"/>
    public static void FitNextItem(string label, float minWidth = 90f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X;
        ImGui.SetNextItemWidth(MathF.Max(minWidth,
            ImGui.GetContentRegionAvail().X - labelWidth - ImGui.GetStyle().ItemInnerSpacing.X));
    }

    /// <summary>Disabled-styled hint text that wraps at the panel edge. TextDisabled/TextColored
    /// never wrap - one long line widens the panel's content region past its fixed width, so it
    /// either clips or drags the panel into sideways scrolling.</summary>
    public static void WrappedHint(ReadOnlySpan<byte> text) => WrappedText(ImGui.GetColorU32(ImGuiCol.TextDisabled), text);

    /// <inheritdoc cref="WrappedHint(System.ReadOnlySpan{byte})"/>
    public static void WrappedHint(string text) => WrappedText(ImGui.GetColorU32(ImGuiCol.TextDisabled), text);

    /// <summary>Normal-colored counterpart of <see cref="WrappedHint(System.ReadOnlySpan{byte})"/> for
    /// dynamic content (names, descriptions) whose length isn't under our control.</summary>
    public static void WrappedLabel(string text) => WrappedText(ImGui.GetColorU32(ImGuiCol.Text), text);

    /// <summary>Warning-colored counterpart of <see cref="WrappedHint(System.ReadOnlySpan{byte})"/>.</summary>
    public static void WrappedWarning(ReadOnlySpan<byte> text) => WrappedText(ImGui.ColorConvertFloat4ToU32(EditorTheme.Warning), text);

    /// <inheritdoc cref="WrappedWarning(System.ReadOnlySpan{byte})"/>
    public static void WrappedWarning(string text) => WrappedText(ImGui.ColorConvertFloat4ToU32(EditorTheme.Warning), text);

    private static void WrappedText(uint color, ReadOnlySpan<byte> text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
    }

    private static void WrappedText(uint color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
    }

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
            ImGui.InputTextWithHint($"##{label}filter", "filter..."u8, ref filter, 128);
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
        ImGui.InputTextWithHint("##loadfilter"u8, "filter by id or name..."u8, ref filter, 128);
        popupFilters[modalTitle] = filter;

        bool pickedAny = false;
        if (ImGui.BeginListBox("##loadrows"u8, new Vector2(340, 260)))
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
                ImGui.TextDisabled("Nothing to load."u8);
            else if (shown == 0)
                ImGui.TextDisabled($"Nothing matches \"{filter}\"");
            ImGui.EndListBox();
        }
        if (ImGui.Button("Cancel"u8, new Vector2(120, 0)))
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
        bool submitted = ImGui.InputTextWithHint("##createname"u8, nameHint, ref nameBuffer, 128,
            ImGuiInputTextFlags.EnterReturnsTrue);

        string? result = null;
        bool nameEmpty = string.IsNullOrWhiteSpace(nameBuffer);
        ImGui.BeginDisabled(nameEmpty);
        if (ImGui.Button("Create"u8, new Vector2(120, 0)) || (submitted && !nameEmpty))
        {
            result = nameBuffer;
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8, new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
        return result;
    }
}
