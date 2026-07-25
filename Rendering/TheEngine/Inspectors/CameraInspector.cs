using Hexa.NET.ImGui;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Inspectors;

public class CameraInspector : IInspectorDrawer<Camera>
{
    public void Draw(Camera component)
    {
        ImGui.Columns(2, "camera_inspector"u8, false);

        ImGuiEx.TextUnformatted("Transform\0"u8);
        ImGui.NextColumn();
        var localToWorld = component.InverseViewMatrix;
        if (InspectorUtils.EditTRS(ref localToWorld))
        {
            component.Transform.Position = localToWorld.Translation;
            component.Transform.Rotation = localToWorld.Rotation();
            component.Transform.Scale = localToWorld.ScaleVector();
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Field of View\0"u8);
        ImGui.NextColumn();
        var fov = component.FOV;
        if (ImGui.SliderFloat("##fov"u8, ref fov, 0.1f, 179.9f))
        {
            component.FOV = fov;
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Near clip\0"u8);
        ImGui.NextColumn();
        var nearClip = component.NearClip;
        if (ImGui.InputFloat("##nearClip"u8, ref nearClip, 1, 10))
        {
            component.NearClip = Math.Max(1, nearClip);
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Far clip\0"u8);
        ImGui.NextColumn();
        var farClip = component.FarClip;
        if (ImGui.InputFloat("##farClip"u8, ref farClip, 1, 10))
        {
            component.FarClip = Math.Max(nearClip + 0.1f, farClip);
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Aspect\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted(component.Aspect.ToString("0.00"));
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}