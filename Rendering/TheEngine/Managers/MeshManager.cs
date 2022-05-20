//#define TRACK_ALLOCATIONS

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
        
        #if TRACK_ALLOCATIONS
        private List<System.Diagnostics.StackTrace> allocations = new();
        #endif

        internal MeshManager(Engine engine)
        {
            meshes = new List<Mesh>();
            this.engine = engine;
        }

        // @todo: not thread safe!
        public IMesh CreateMesh(Vector3[] vertices, ushort[] indices)
        {
            var handle = new MeshHandle(meshes.Count);

            var mesh = new Mesh(engine, handle, false);
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, 0);
            mesh.Rebuild();

            meshes.Add(mesh);
            #if TRACK_ALLOCATIONS
            allocations.Add(new System.Diagnostics.StackTrace(2));
            #endif

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
                    uv1 = meshData.UV == null ? Vector2.Zero : meshData.UV[i],
                    uv2 = meshData.UV2 == null ? Vector2.Zero : meshData.UV2[i],
                    color = meshData.Colors == null ? Vector4.Zero : meshData.Colors[i]
                };
            }
            
            var mesh = new Mesh(engine, handle, vertices, meshData.Indices, meshData.IndicesCount, true, false);
            //ArrayPool<UniversalVertex>.Shared.Return(vertices);
            meshes.Add(mesh);

#if TRACK_ALLOCATIONS
            allocations.Add(new System.Diagnostics.StackTrace(2));
#endif
            return mesh;
        }
        
        public void DisposeMesh(IMesh mesh)
        {
            var index = meshes.IndexOf((Mesh)mesh);
            ((Mesh)mesh).Dispose();
            meshes[index] = null!;
#if TRACK_ALLOCATIONS
            allocations[index] = null!;
#endif
        }

        public IMesh CreateManagedOnlyMesh(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<ushort> indices)
        {
            var handle = new MeshHandle(meshes.Count);

            var mesh = new Mesh(engine, handle, true);
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, 0);
            mesh.BuildBoundingBox();

            meshes.Add(mesh);
            #if TRACK_ALLOCATIONS
            allocations.Add(null!);
            #endif

            return mesh;
        }

        public void Dispose()
        {
            for (var index = 0; index < meshes.Count; index++)
            {
                var mesh = meshes[index];
                if (mesh == null || mesh.IsManagedOnly)
                    continue;
#if TRACK_ALLOCATIONS
                Console.WriteLine("Mesh not disposed! Allocated here: ");                
                Console.WriteLine(allocations[index]);
#else
                Console.WriteLine("Mesh not disposed!");
#endif
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
