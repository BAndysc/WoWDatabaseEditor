using System.Numerics;
using Hexa.NET.ImGui;

namespace WDE.MapRenderer.Utils;

/// <summary>
/// The shared mechanics behind every in-view vector-icon button (the spawn tool strip, the
/// view-settings strip): invisible button + hover/active/enabled background + the glyph box
/// convention (unit box, 16% inset, stroke ~9% of the box). The GLYPHS stay with their owners -
/// this returns where and how to draw one, so callers pay no per-frame delegate allocation.
/// </summary>
public static class ImGuiIconButtons
{
    /// <summary>Distance between the view edge and the OUTER edge (backdrop included) of every
    /// in-view overlay - the tool strip, the view-settings strip, the inspector, the stats panel
    /// and the camera coordinates box all share it so their edges line up.</summary>
    public const float ViewMargin = 7f;

    public readonly struct IconButtonSlot
    {
        public required bool Clicked { get; init; }
        public required Vector2 GlyphOrigin { get; init; }
        public required float GlyphSize { get; init; }
        public required uint GlyphColor { get; init; }
    }

    /// <summary>Stroke thickness for a glyph authored in a box of size <paramref name="s"/> -
    /// the one convention all the icon sets share.</summary>
    public static float Stroke(float s) => MathF.Max(1.4f, s * 0.09f);

    /// <summary>Button chrome + tooltip; draw the glyph at the returned slot. Returns
    /// Clicked=false while disabled (the tooltip still shows - it explains WHY it's disabled).</summary>
    public static IconButtonSlot IconButton(string id, float size, bool active, string? tooltip,
        bool enabled = true, uint activeColor = 0)
    {
        var pos = ImGui.GetCursorScreenPos();
        if (!enabled)
            ImGui.BeginDisabled();
        bool clicked = ImGui.InvisibleButton(id, new Vector2(size, size));
        if (!enabled)
            ImGui.EndDisabled();

        bool hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
        bool held = enabled && ImGui.IsItemActive();

        var dl = ImGui.GetWindowDrawList();
        uint bg = active ? activeColor
            : held ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
            : hovered && enabled ? ImGui.GetColorU32(ImGuiCol.ButtonHovered)
            : 0;
        if (bg != 0)
            dl.AddRectFilled(pos, pos + new Vector2(size, size), bg, 4f);

        if (hovered && tooltip != null)
            ImGui.SetTooltip(tooltip);

        float inset = size * 0.16f;
        return new IconButtonSlot
        {
            Clicked = clicked && enabled,
            GlyphOrigin = pos + new Vector2(inset, inset),
            GlyphSize = size - inset * 2,
            GlyphColor = ImGui.GetColorU32(enabled ? ImGuiCol.Text : ImGuiCol.TextDisabled),
        };
    }

    /// <summary>The little dropdown caret in a button's bottom-right corner - pass the button's
    /// rect max (drawn identically by every strip's dropdown buttons).</summary>
    public static void DropdownCaret(ImDrawListPtr dl, Vector2 buttonRectMax, uint color)
    {
        dl.AddTriangleFilled(
            new Vector2(buttonRectMax.X - 9, buttonRectMax.Y - 4),
            new Vector2(buttonRectMax.X - 3, buttonRectMax.Y - 4),
            new Vector2(buttonRectMax.X - 6, buttonRectMax.Y - 1),
            color);
    }

    /// <summary>The translucent rounded backdrop every in-view strip/pill sits on.</summary>
    public static void Backdrop(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding = 6f)
    {
        dl.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.WindowBg, 0.80f), rounding);
        dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border, 0.6f), rounding);
    }
}
