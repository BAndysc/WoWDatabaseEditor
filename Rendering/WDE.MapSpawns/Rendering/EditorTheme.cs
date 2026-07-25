using System.Numerics;
using Hexa.NET.ImGui;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// The spawn editor's semantic colors - one place instead of per-file literals, so every panel
/// means the same thing by "warning amber" or "destructive red". World-decal colors stay with
/// their tools (the same violet means different things per tool, see the per-panel legends).
/// </summary>
public static class EditorTheme
{
    /// <summary>Active tool button, primary interactive highlights.</summary>
    public static readonly Vector4 Accent = new(0.20f, 0.45f, 0.85f, 1f);

    /// <summary>Unsaved-changes dots, advisory warnings, refused-keypress flashes.</summary>
    public static readonly Vector4 Warning = new(1f, 0.75f, 0.25f, 1f);

    /// <summary>The selected thing's name/title.</summary>
    public static readonly Vector4 SelectionGold = new(1f, 0.8f, 0.2f, 1f);

    /// <summary>Error/deleted text (readable on the dark background, not a pure red).</summary>
    public static readonly Vector4 DangerText = new(1f, 0.5f, 0.5f, 1f);

    /// <summary>Clickable jump-to-editor text rows (a light accent so they read as links).</summary>
    public static readonly Vector4 LinkText = new(0.55f, 0.75f, 1f, 1f);

    /// <summary>A small dimmed "(?)" whose tooltip carries explanatory text that would otherwise
    /// permanently occupy panel space. Call right after the element it explains (same line).</summary>
    public static void HelpMarker(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled(TheEngine.Lucide.CircleHelp);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(text);
    }

    /// <summary>Armed/confirmed "go" state (waypoint pen mode's active button).</summary>
    public static readonly Vector4 ArmedGreen = new(0.16f, 0.55f, 0.25f, 1f);
    public static readonly Vector4 ArmedGreenHovered = new(0.20f, 0.65f, 0.30f, 1f);
    public static readonly Vector4 ArmedGreenActive = new(0.13f, 0.45f, 0.21f, 1f);

    public static readonly Vector4 DestructiveButton = new(0.55f, 0.16f, 0.16f, 1f);
    public static readonly Vector4 DestructiveButtonHovered = new(0.75f, 0.22f, 0.22f, 1f);
    public static readonly Vector4 DestructiveButtonActive = new(0.85f, 0.28f, 0.28f, 1f);

    /// <summary>Translucent backdrop of every in-view chrome piece (strips, pills, tabs).</summary>
    public const float BackdropAlpha = 0.80f;
    public const float BackdropBorderAlpha = 0.6f;

    /// <summary>The red button family for deletes/removals - pair with <see cref="PopButtonColors"/>.</summary>
    public static void PushDestructiveButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, DestructiveButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DestructiveButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, DestructiveButtonActive);
    }

    /// <summary>The green button family for armed "click the world now" modes.</summary>
    public static void PushArmedButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ArmedGreen);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ArmedGreenHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ArmedGreenActive);
    }

    public static void PopButtonColors() => ImGui.PopStyleColor(3);
}
