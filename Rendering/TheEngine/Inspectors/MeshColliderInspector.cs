using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class MeshColliderInspector : IRefInspectorDrawer<MeshCollider>
{
    private readonly Engine engine;

    public MeshColliderInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref MeshCollider component)
    {
        Mesh? mesh = null;
        try
        {
            mesh = engine.meshManager.GetMeshByHandle(component.Mesh);
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        ImGui.Columns(2, "meshcollider", false);

        ImGuiEx.TextUnformatted("Mesh\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted(mesh == null ? "(none assigned)" : $"Handle #{component.Mesh.Handle}, {mesh.SubmeshCount} submesh(es)");
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Submesh\0"u8);
        ImGui.NextColumn();
        if (mesh != null)
            ImGui.SliderInt("##SubMesh", ref component.SubMesh, 0, Math.Max(0, mesh.SubmeshCount - 1));
        else
            ImGui.InputInt("##SubMesh", ref component.SubMesh);
        ImGui.NextColumn();

        ImGui.Columns(1);

        ImGuiEx.TextUnformatted("Static-only triangle-soup collider; RigidBody on this entity is ignored\0"u8);
    }
}
