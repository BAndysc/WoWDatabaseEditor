using System;
using System.Collections.Generic;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IMesh
    {
        void SetIndices(ReadOnlySpan<uint> indices, int submesh);
        void SetSubmeshIndicesRange(int submesh, int start, int length);
        void Rebuild();
        void Activate();

        void SetSubmeshCount(int count);
        int IndexCount(int submesh);
        int IndexStart(int submesh);
        int SubmeshCount { get; }
        
        BoundingBox Bounds { get; }

        MeshHandle Handle { get; }
        IEnumerable<(Vector4, Vector4, Vector4)> GetFaces(int submesh);
    }
}
