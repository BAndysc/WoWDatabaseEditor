using System.Numerics;
using Hexa.NET.ImGui;

namespace WDE.MapSpawns.Rendering;

/// <summary>A transient orange "your value was adjusted" advisory - numeric fields that silently
/// clamp call <see cref="Set"/> with a message naming the field and the range; the owning panel
/// calls <see cref="Draw"/> once per frame (typically at its bottom) and the note fades out after
/// a few seconds. One instance per inspector.</summary>
public sealed class ClampNote
{
    private const float VisibleSeconds = 4f;

    private string? text;
    private double shownAt;

    public void Set(string text)
    {
        this.text = text;
        shownAt = ImGui.GetTime();
    }

    public void Draw()
    {
        if (text == null)
            return;
        if (ImGui.GetTime() - shownAt > VisibleSeconds)
        {
            text = null;
            return;
        }
        ImGui.TextColored(EditorTheme.Warning, text);
    }
}
