using TheEngine.Data;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IMeshManager
    {
        IMesh CreateMesh(Vector3[] vertices, int[] indices);
        IMesh CreateMesh(in MeshData mesh);
        void DisposeMesh(IMesh mesh);
        IMesh CreateManagedOnlyMesh(Vector3[] vertices, int[] indices);
    }
}
