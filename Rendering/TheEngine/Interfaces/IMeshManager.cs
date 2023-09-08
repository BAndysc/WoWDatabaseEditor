using TheEngine.Data;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IMeshManager
    {
        IMesh CreateMesh(Vector3[] vertices, ushort[] indices);
        IMesh CreateMesh(Vector3[] vertices, uint[] indices);
        IMesh CreateMesh(in MeshData mesh);
        void DisposeMesh(IMesh mesh);
        IMesh CreateManagedOnlyMesh(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<ushort> indices);
    }
}
