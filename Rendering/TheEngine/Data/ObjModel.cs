using System;
using System.Collections.Generic;
using TheMaths;

namespace TheEngine.Data
{
    public class ObjModel
    {
        public IList<Vector3> Vertices { get; }
        public IList<Vector3> Normals { get; }
        public IList<Vector2> UV { get; }
        public IList<Face> Faces { get; }

        public ObjModel(IList<Vector3> vertices, IList<Vector3> normals, IList<Vector2> uvs, List<Face> faces)
        {
            Vertices = vertices;
            Normals = normals;
            UV = uvs;
            Faces = faces;

            GenerateMeshData();
        }

        public class Face
        {
            public Node[] Nodes { get; }

            public Face(Node node1, Node node2, Node node3)
            {
                Nodes = new Node[] { node1, node2, node3 };
            }

            public Face(params Node[] nodes)
            {
                if (nodes.Length != 3)
                    throw new Exception("Face is expected to be a triangle!");

                Nodes = nodes;
            }
        }

        public class Node
        {
            public int Vertex { get; }
            public int Normal { get; }
            public int UV { get; }
            
            public Node(int vertex, int normal, int uv)
            {
                Vertex = vertex;
                Normal = normal;
                UV = uv;
            }

            public override bool Equals(object? obj)
            {
                var node = obj as Node;
                return node != null &&
                       Vertex == node.Vertex &&
                       Normal == node.Normal &&
                       UV == node.UV;
            }

            public override int GetHashCode()
            {
                var hashCode = 1748542731;
                hashCode = hashCode * -1521134295 + Vertex.GetHashCode();
                hashCode = hashCode * -1521134295 + Normal.GetHashCode();
                hashCode = hashCode * -1521134295 + UV.GetHashCode();
                return hashCode;
            }
        }

        public MeshData MeshData { get; private set; }
        
        private void GenerateMeshData()
        {
            List<Vector3> realVertices = new List<Vector3>();
            List<Vector3> realNormals = new List<Vector3>();
            List<Vector2> realUVs = new List<Vector2>();

            List<ushort> indices = new List<ushort>();

            ushort vertexCount = 0;

            Dictionary<Node, ushort> nodeToId = new Dictionary<Node, ushort>();

            foreach (var face in Faces)
            {
                for (int i = 0; i < 3; ++i)
                {
                    var node = face.Nodes[i];
                    if (!nodeToId.ContainsKey(node))
                    {
                        nodeToId[node] = vertexCount++;
                        
                        if (vertexCount == ushort.MaxValue)
                            throw new Exception("Too many vertices!");

                        realVertices.Add(Vertices[node.Vertex - 1]);
                        realNormals.Add(Normals[node.Normal - 1]);
                        realUVs.Add(UV[node.UV - 1]);
                    }
                }

                for (int i = 0; i < 3; ++i)
                    indices.Add(nodeToId[face.Nodes[i]]);
            }

            MeshData = new MeshData(realVertices.ToArray(), realNormals.ToArray(), realUVs.ToArray(), indices.ToArray());
        }
    }
}
