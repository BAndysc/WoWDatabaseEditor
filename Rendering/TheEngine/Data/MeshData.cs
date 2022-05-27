using TheMaths;

namespace TheEngine.Data
{
    public readonly struct MeshData
    {
        public readonly Vector3[] Vertices { get; }
        public readonly Vector3[]? Normals { get; }
        public readonly Vector2[]? UV { get; }
        public readonly Vector2[]? UV2 { get; }
        public readonly Color[]? Colors { get; }
        public readonly Color[]? Colors2 { get; }
        public readonly ushort[] Indices { get; }
        public readonly int VerticesCount { get; }
        public readonly int IndicesCount { get; }

        public MeshData(Vector3[] vertices, 
            Vector3[]? normals, 
            Vector2[]? uvs, 
            ushort[] indices,
            int? verticesCount = null,
            int? indicesCount = null,
            Vector2[]? uvs2 = null,
            Color[]? colors = null,
            Color[]? colors2 = null)
        {
            Vertices = vertices;
            Normals = normals;
            UV = uvs;
            UV2 = uvs2;
            Indices = indices;
            VerticesCount = verticesCount ?? vertices.Length;
            IndicesCount = indicesCount ?? indices.Length;
            Colors = colors;
            Colors2 = colors2;
        }
    }
}
