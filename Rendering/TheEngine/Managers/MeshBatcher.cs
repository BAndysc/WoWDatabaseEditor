using System.Diagnostics;

namespace TheEngine.Managers;

public class MeshBatcher
{
    public static (Vector3[], ushort[]) MergeMeshes(IReadOnlyList<Vector3[]> vertices, IReadOnlyList<ushort[]> indices, IReadOnlyList<Vector3> vertexOffsets)
    {
        int totalVertices = 0;
        int totalIndices = 0;
        int meshes = vertices.Count;
        Debug.Assert(meshes == indices.Count);

        for (int i = 0; i < meshes; ++i)
        {
            totalVertices += vertices[i].Length;
            totalIndices += indices[i].Length;
        }

        if (totalVertices > ushort.MaxValue)
            throw new Exception("More vertices than the engine can handle now :(");
        
        Vector3[] mergedVertices = new Vector3[totalVertices];
        ushort[] mergedIndices = new ushort[totalIndices];

        int vertexOffset = 0;
        int indexOffset = 0;
        for (int i = 0; i < meshes; ++i)
        {
            var offset = vertexOffsets[i];
            for (int v = 0; v < vertices[i].Length; ++v)
            {
                mergedVertices[vertexOffset + v] = vertices[i][v] + offset;
            }
            for (int j = 0; j < indices[i].Length; ++j)
            {
                mergedIndices[indexOffset++] = (ushort)(indices[i][j] + vertexOffset);
            }

            vertexOffset += vertices[i].Length;
            totalIndices += indices[i].Length;
        }

        return (mergedVertices, mergedIndices);
    }
}