using Silk.NET.Vulkan;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IMesh
    {
        void SetIndices(ReadOnlySpan<ushort> indices, int submesh);
        void SetSubmeshIndicesRange(int submesh, int start, int length);
        void RebuildIndices();

        void SetSubmeshCount(int count);
        int IndexCount(int submesh);
        int IndexStart(int submesh);
        int SubmeshCount { get; }
        
        BoundingBox Bounds { get; }

        MeshHandle Handle { get; }
        IndexType IndexType { get; }
        IEnumerable<(Vector4, Vector4, Vector4)> GetFaces(int submesh);
        void SaveToObj(string path);
    }
}
