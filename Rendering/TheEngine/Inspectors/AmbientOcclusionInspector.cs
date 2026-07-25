using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class AmbientOcclusionInspector : IRefInspectorDrawer<AmbientOcclusion>
{
    private readonly Engine engine;

    public AmbientOcclusionInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref AmbientOcclusion component)
    {
        ImGui.Columns(2, "camera_inspector", false);

        ImGuiEx.TextUnformatted("Is Enabled\0"u8);
        ImGui.NextColumn();
        ImGui.Checkbox("##enabled", ref component.Enabled);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Radius\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##radius", ref component.Radius, 0, 2);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Intensity\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##intensity", ref component.Intensity, 0, 5);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Power\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##power", ref component.Power, 0, 10);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Bias\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##bias", ref component.Bias, 0, 2);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Min Distance\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##mindist", ref component.MinDist, 0, 200);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Max Distance\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##maxdist", ref component.MaxDist, 0, 200);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}