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

        private List<WeakReference<Mesh>> meshes;

        private int disposeIndex = 0;
        private List<Mesh>[] disposeLists = [new(), new()];

        #if TRACK_ALLOCATIONS
        private List<System.Diagnostics.StackTrace> allocations = new();
        #endif

        internal MeshManager(Engine engine)
        {
            meshes = new List<WeakReference<Mesh>>();
            this.engine = engine;
        }

        // @todo: not thread safe!
        public IMesh CreateMesh(Vector3[] vertices, ushort[] indices)
        {
            var handle = new MeshHandle(meshes.Count);

            var mesh = new Mesh(engine, handle, false);
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, 0);
            mesh.RebuildIndices();

            meshes.Add(new WeakReference<Mesh>(mesh));
            #if TRACK_ALLOCATIONS
            allocations.Add(new System.Diagnostics.StackTrace(2));
            #endif

            return mesh;
        }

        // @todo: not thread safe!
        public IMesh CreateMesh(Vector3[] vertices, uint[] indices)
        {
            var handle = new MeshHandle(meshes.Count);

            var mesh = new Mesh(engine, handle, false);
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, 0);
            mesh.RebuildIndices();

            meshes.Add(new WeakReference<Mesh>(mesh));
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
                    position = meshData.Vertices[i],
                    normal = meshData.Normals == null ? Vector3.One : meshData.Normals[i],
                    uv1 = meshData.UV == null ? Vector2.Zero : meshData.UV[i],
                    uv2 = meshData.UV2 == null ? Vector2.Zero : meshData.UV2[i],
                    color = meshData.Colors == null ? 0 : meshData.Colors[i].ToRgba(),
                    color2 = meshData.Colors2 == null ? 0 : meshData.Colors2[i].ToRgba()
                };
            }
            
            var mesh = new Mesh(engine, handle, vertices, meshData.Indices, meshData.IndicesCount, true, false);
            //ArrayPool<UniversalVertex>.Shared.Return(vertices);
            meshes.Add(new WeakReference<Mesh>(mesh));

#if TRACK_ALLOCATIONS
            allocations.Add(new System.Diagnostics.StackTrace(2));
#endif
            return mesh;
        }
        
        public void DisposeMesh(IMesh mesh)
        {
            if (!meshes[mesh.Handle.Handle].TryGetTarget(out var storedMesh))
                throw new Exception("Invalid handle to dispose!");
            if (storedMesh != mesh)
                throw new Exception("Invalid handle to dispose 2!");
            ((Mesh)mesh).Dispose();
            meshes[mesh.Handle.Handle] = null!;
#if TRACK_ALLOCATIONS
            allocations[mesh.Handle.Handle] = null!;
#endif
        }

        public IMesh CreateManagedOnlyMesh(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<ushort> indices)
        {
            var handle = new MeshHandle(meshes.Count);

            var mesh = new Mesh(engine, handle, true);
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, 0);
            mesh.BuildBoundingBox();

            meshes.Add(new WeakReference<Mesh>(mesh));
            #if TRACK_ALLOCATIONS
            allocations.Add(null!);
            #endif

            return mesh;
        }

        public void Dispose()
        {
            for (var index = 0; index < meshes.Count; index++)
            {
                var meshRef = meshes[index];
                if (meshRef == null || !meshRef.TryGetTarget(out var mesh))
                    continue;
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
            if (meshes[mesh.Handle].TryGetTarget(out var meshInstane))
                return meshInstane;
            return null;
        }

        internal void Update()
        {
            List<Mesh> toDispose;
            lock (this)
            {
                toDispose = disposeLists[disposeIndex];
                disposeIndex = 1 - disposeIndex;
            }

            foreach (var mesh in toDispose)
            {
                if (mesh.IsManagedOnly)
                    continue;

                meshes[mesh.Handle.Handle] = null!;
                mesh.Dispose();
            }
            toDispose.Clear();
        }

        internal void AddToDisposeList(Mesh mesh)
        {
            lock (this)
            {
                disposeLists[disposeIndex].Add(mesh);
            }
        }
    }
}
