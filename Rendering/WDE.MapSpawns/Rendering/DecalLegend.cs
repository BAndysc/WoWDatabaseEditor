using System.Numerics;
using Hexa.NET.ImGui;

namespace WDE.MapSpawns.Rendering;

/// <summary>A compact "what do the ground markers mean" legend for the group/pool panels.
/// The same violet means formation ghost slots in one tool and entry-wide pooling in the other,
/// so each panel names its own colors.</summary>
public static class DecalLegend
{
    public static void Draw(params (Vector4 color, string label)[] entries)
    {
        ImGui.Separator();
        ImGui.TextDisabled("Markers:"u8);
        float right = ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X;
        foreach (var (color, label) in entries)
        {
            float itemWidth = ImGui.GetFontSize() * 0.9f + ImGui.CalcTextSize(label).X + 8;
            ImGui.SameLine(0, 8);
            if (ImGui.GetCursorPosX() + itemWidth > right)
                ImGui.NewLine();
            var pos = ImGui.GetCursorScreenPos();
            float radius = ImGui.GetFontSize() * 0.30f;
            ImGui.GetWindowDrawList().AddCircleFilled(
                new Vector2(pos.X + radius, pos.Y + ImGui.GetTextLineHeight() * 0.55f), radius, ImGui.GetColorU32(color));
            ImGui.Dummy(new Vector2(radius * 2 + 3, ImGui.GetTextLineHeight()));
            ImGui.SameLine(0, 3);
            ImGui.TextDisabled(label);
        }
    }
}
