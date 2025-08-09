using ImGuiNET;
using TheMaths;

namespace TheEngine.Inspectors;

public static class InspectorUtils
{
    public static bool EditTRS(ref Matrix trs)
    {
        var position = trs.Translation;
        var rotation = trs.Rotation();
        var scale = trs.ScaleVector();
        bool changed = false;

        if (ImGui.DragFloat3("Position", ref position))
        {
            changed = true;
        }

        var euler = rotation.ToEulerDeg();
        if (ImGui.DragFloat3("Rotation (Euler)", ref euler, 0.1f, 0f, 360f))
        {
            changed = true;
        }

        if (ImGui.DragFloat3("Scale", ref scale))
        {
            changed = true;
        }

        if (changed)
        {
            euler *= MathUtil.Deg2Rad;
            trs = Utilities.TRS(position, Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z), scale);
            return true;
        }

        return false;
    }

}