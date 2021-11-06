using System;
using System.Buffers;
using System.Collections.Generic;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Managers
{
    public class MeshManager : IMeshManager, IDisposable
    {
        private readonly Engine engine;

        private List<Mesh> meshes;

        internal MeshManager(Engine engine)
        {
            meshes = new List<Mesh>();
            this.engine = engine;
        }

        // @todo: not thread safe!
        public IMesh CreateMesh(Vector3[] vertices, int[] indices)
        {
            var handle = new MeshHandle(meshes.Count);

            var mesh = new Mesh(engine, handle);
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, 0);
            mesh.Rebuild();

            meshes.Add(mesh);

            return mesh;
        }

        // @todo: not thread safe!
        public IMesh CreateMesh(in MeshData meshData)
        {
            var handle = new MeshHandle(meshes.Count);

            var vertices = new UniversalVertex[meshData.VerticesCount];// ArrayPool<UniversalVertex>.Shared.Rent(meshData.Vertices.Length);;
            for (int i = 0; i < meshData.VerticesCount; ++i)
            {
                vertices[i] = new UniversalVertex()
                {
                    position = new Vector4(meshData.Vertices[i], 1),
                    normal = meshData.Normals == null ? Vector4.One : new Vector4(meshData.Normals[i], 0),
                    uv1 = meshData.UV == null ? Vector2.Zero : meshData.UV[i]
                };
            }
            
            var mesh = new Mesh(engine, handle, vertices, meshData.Indices, meshData.IndicesCount, true);
            //ArrayPool<UniversalVertex>.Shared.Return(vertices);
            meshes.Add(mesh);

            return mesh;
        }
        
        public void DisposeMesh(IMesh mesh)
        {
            ((Mesh)mesh).Dispose();
            meshes[meshes.IndexOf((Mesh)mesh)] = null!;
        }

        public void Dispose()
        {
            foreach (var mesh in meshes)
            {
                if (mesh == null)
                    continue;
                Console.WriteLine("Mesh not disposed!");
                mesh.Dispose();
            }

            meshes.Clear();
        }

        internal Mesh GetMeshByHandle(MeshHandle mesh)
        {
            return meshes[mesh.Handle];
        }
    }
}
