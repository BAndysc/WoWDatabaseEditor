using Hexa.NET.ImGui;
using TheEngine.Managers;
using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Inspectors;

public class DrawTextDataInspector : IInspectorDrawer<UIManager.DrawTextData>
{
    public void Draw(UIManager.DrawTextData component)
    {
        ImGui.Columns(2, "drawtextdata_inspector", false);

        ImGuiEx.TextUnformatted("Font\0"u8);
        ImGui.NextColumn();
        var font = component.font ?? "";
        if (ImGui.InputText("##font", ref font, 256))
            component.font = font;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Text\0"u8);
        ImGui.NextColumn();
        var text = component.text ?? "";
        if (ImGui.InputText("##text", ref text, 1024))
            component.text = text;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Font Size\0"u8);
        ImGui.NextColumn();
        var fontSize = component.fontSize;
        if (ImGui.InputFloat("##fontSize", ref fontSize, 0.5f, 1.0f))
            component.fontSize = fontSize;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Pivot\0"u8);
        ImGui.NextColumn();
        var pivot = component.pivot;
        if (ImGui.InputFloat2("##pivot", ref pivot))
            component.pivot = pivot;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Visibility Distance\0"u8);
        ImGui.NextColumn();
        var visDist = MathF.Sqrt(component.visibilityDistanceSquare);
        if (ImGui.InputFloat("##visDist", ref visDist, 1f, 10f))
            component.visibilityDistanceSquare = visDist * visDist;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Font Color\0"u8);
        ImGui.NextColumn();
        ImGui.ColorEdit4("##fontColor", ref component.fontColor);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Background Color\0"u8);
        ImGui.NextColumn();
        bool hasBackground = component.backgroundColor.HasValue;
        if (ImGui.Checkbox("##hasBg", ref hasBackground))
            component.backgroundColor = hasBackground ? Vector4.One : null;
        if (component.backgroundColor.HasValue)
        {
            ImGui.SameLine();
            var bgColor = component.backgroundColor.Value;
            if (ImGui.ColorEdit4("##bgColor", ref bgColor))
                component.backgroundColor = bgColor;
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
