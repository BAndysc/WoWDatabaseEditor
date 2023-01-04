using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGLBindings;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Entities
{
    internal class Mesh : IMesh, IDisposable
    {
        private readonly Engine engine;
        private readonly bool managedOnly;

        internal int VertexArrayObject { get; private set; }
        internal NativeBuffer<UniversalVertex>? VerticesBuffer { get; private set; }
        internal NativeBuffer<byte>? IndicesBuffer { get; private set; }

        private BoundingBox bounds;
        private Vector3[]? positions = null;
        private ushort[]? shortIndices;
        private uint[]? bigIndices;
        private int indicesCount = 0;
        private int verticesCount = 0;
        private (int start, int length)[]? submeshesRange;

        public BoundingBox Bounds => bounds;
        
        public IndexType IndexType => bigIndices != null ? IndexType.Int : IndexType.Short;
        
        public void Activate()
        {
            Debug.Assert(!managedOnly);
            engine.Device.device.BindVertexArray(VertexArrayObject);
            VerticesBuffer!.Activate(0);
            IndicesBuffer!.Activate(0);
        }

        public int IndexCount(int submesh)
        {
            if (submeshesRange == null && submesh == 0)
                return indicesCount;
            if (submeshesRange == null && submesh != 0)
                throw new Exception($"There is not submesh {submesh}");
            if (submeshesRange != null && submesh >= submeshesRange.Length)
                throw new Exception($"There is not submesh {submesh}");
            return submeshesRange[submesh].length;
        }

        public int IndexStart(int submesh)
        {
            if (submeshesRange == null && submesh == 0)
                return 0;
            if (submeshesRange == null && submesh != 0)
                throw new Exception($"There is not submesh {submesh}");
            if (submeshesRange != null && submesh >= submeshesRange.Length)
                throw new Exception($"There is not submesh {submesh}");
            return submeshesRange[submesh].start;
        }

        public MeshHandle Handle { get; }
        
        public IEnumerable<(Vector4, Vector4, Vector4)> GetFaces(int submesh)
        {
            if (disposed)
                throw new Exception("Mesh is disposed");
            Debug.Assert(positions != null);
            Debug.Assert(shortIndices != null || bigIndices != null);
            int start = IndexStart(submesh);
            int count = IndexCount(submesh);
            for (int i = start; i + 2 < start + count; i += 3)
            {
                uint j = shortIndices != null ? shortIndices[i] : bigIndices![i];
                uint k = shortIndices != null ? shortIndices[i+1] : bigIndices![i+1];
                uint l = shortIndices != null ? shortIndices[i+2] : bigIndices![i+2];
                
                if (verticesCount > j &&
                    verticesCount > k &&
                    verticesCount > l)
                {
                    var v1 = positions[j];
                    var v2 = positions[k];
                    var v3 = positions[l];
                    yield return (new Vector4(v1, 1), new Vector4(v2, 1), new Vector4(v3, 1));
                }
            }
        }

        internal Mesh(Engine engine, MeshHandle handle, bool managedOnly)
        {
            this.engine = engine;
            this.managedOnly = managedOnly;
            Handle = handle;
            indicesCount = 0;
            if (!managedOnly)
            {
                VerticesBuffer = engine.Device.CreateBuffer<UniversalVertex>(BufferTypeEnum.Vertex, 1);
                IndicesBuffer = engine.Device.CreateBuffer<byte>(BufferTypeEnum.Index, 4);
                SetupDeviceBuffers(engine);
            }
        }

        internal Mesh(Engine engine, MeshHandle handle, UniversalVertex[] vertices, ushort[] indices, int indicesCount, bool ownVerticesArray, bool managedOnly)
        {
            this.engine = engine;
            Handle = handle;
            positions = vertices.Select(x => x.position).ToArray();
            this.verticesCount = vertices.Length;
            this.shortIndices = indices;
            this.indicesCount = indicesCount;
            this.managedOnly = managedOnly;
            BuildBoundingBox();
            if (!managedOnly)
            {
                VerticesBuffer = engine.Device.CreateBuffer<UniversalVertex>(BufferTypeEnum.Vertex, vertices);
                IndicesBuffer = engine.Device.CreateBuffer<byte>(BufferTypeEnum.Index, MemoryMarshal.AsBytes(indices.AsSpan(0, indicesCount)));
                SetupDeviceBuffers(engine);
            }
        }

        private void SetupDeviceBuffers(Engine engine)
        {
            VertexArrayObject = engine.Device.device.GenVertexArray();
            engine.Device.device.BindVertexArray(VertexArrayObject);
            VerticesBuffer.Activate(0);
            IndicesBuffer.Activate(0);
            int stride = 3 * 4 + 3 * 4 + 2 * 4 + 2 * 4 + 4 + 4; // 4 * 4 * 3 + 2 * 4 * 2;
            engine.Device.device.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, new IntPtr(0));
            engine.Device.device.EnableVertexAttribArray(0);

            engine.Device.device.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, new IntPtr(12));
            engine.Device.device.EnableVertexAttribArray(1);

            engine.Device.device.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(24));
            engine.Device.device.EnableVertexAttribArray(2);

            engine.Device.device.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(32));
            engine.Device.device.EnableVertexAttribArray(3);

            engine.Device.device.VertexAttribPointer(4, 4, VertexAttribPointerType.UnsignedByte, true, stride, new IntPtr(40));
            engine.Device.device.EnableVertexAttribArray(4);

            engine.Device.device.VertexAttribPointer(5, 4, VertexAttribPointerType.UnsignedByte, true, stride, new IntPtr(44));
            engine.Device.device.EnableVertexAttribArray(5);
            engine.Device.device.BindVertexArray(0);
        }

        public void SetSubmeshCount(int count)
        {
            if (count == 1)
                submeshesRange = null;
            else
                submeshesRange = new (int, int)[count];
        }

        public int SubmeshCount => submeshesRange?.Length ?? 1;
        public bool IsManagedOnly => managedOnly;

        public void SetSubmeshIndicesRange(int submesh, int start, int length)
        {
            Debug.Assert((shortIndices?.Length ?? bigIndices!.Length) >= start + length);
            if (submeshesRange == null && submesh == 0)
            {
                if (start == 0 && length == (shortIndices?.Length ?? bigIndices!.Length))
                {
                    // ok
                }
                else
                {
                    submeshesRange = new (int, int)[1];
                    submeshesRange[0] = (start, length);
                }
            }
            else if (submeshesRange == null && submesh != 0)
                throw new Exception("You need to call SetSubmeshCount first!");
            else if (submeshesRange != null && submesh >= submeshesRange.Length)
                throw new Exception("Submesh id out of range");
            else
                submeshesRange[submesh] = (start, length);
        }

        public void SetIndices(ReadOnlySpan<ushort> indices, int submesh)
        {
            if (bigIndices != null)
                throw new Exception("Can't SetIndices(ushort) on a mesh that was created with SetIndices(uint)");
            
            if (submeshesRange == null && submesh == 0)
            {
                this.shortIndices = indices.ToArray();
                indicesCount = indices.Length;
            }
            else if (submeshesRange == null && submesh != 0)
                throw new Exception("You need to call SetSubmeshCount first!");
            else if (submeshesRange != null && submeshesRange.Length > submesh)
            {
                var existingIndices = submeshesRange[submesh];
                if (existingIndices.length == 0) // no indices so far
                {
                    var newIndices = new ushort[indicesCount + indices.Length];
                    Array.Copy(this.shortIndices, newIndices, indicesCount);
                    indices.CopyTo(newIndices.AsSpan(indicesCount, indices.Length));
                    //Array.Copy(indices, 0, newIndices, this.indices.Length, indices.Length);
                    submeshesRange[submesh] = (indicesCount, indices.Length);
                    this.shortIndices = newIndices;
                    indicesCount = newIndices.Length;
                }
                else // for this submesh there are some indices already
                {
                    throw new Exception("sorry, overriding indices with submeshes is not yet supported");
                }
            }
            else
                throw new Exception("Submesh out of range");
        }

        public void SetIndices(ReadOnlySpan<uint> indices, int submesh)
        {
            if (shortIndices != null)
                throw new Exception("Can't SetIndices(ushort) on a mesh that was created with SetIndices(uint)");
            
            if (submeshesRange == null && submesh == 0)
            {
                this.bigIndices = indices.ToArray();
                indicesCount = indices.Length;
            }
            else if (submeshesRange == null && submesh != 0)
                throw new Exception("You need to call SetSubmeshCount first!");
            else if (submeshesRange != null && submeshesRange.Length > submesh)
            {
                var existingIndices = submeshesRange[submesh];
                if (existingIndices.length == 0) // no indices so far
                {
                    var newIndices = new uint[indicesCount + indices.Length];
                    Array.Copy(this.shortIndices, newIndices, indicesCount);
                    indices.CopyTo(newIndices.AsSpan(indicesCount, indices.Length));
                    //Array.Copy(indices, 0, newIndices, this.indices.Length, indices.Length);
                    submeshesRange[submesh] = (indicesCount, indices.Length);
                    this.bigIndices = newIndices;
                    indicesCount = newIndices.Length;
                }
                else // for this submesh there are some indices already
                {
                    throw new Exception("sorry, overriding indices with submeshes is not yet supported");
                }
            }
            else
                throw new Exception("Submesh out of range");
        }
        
        public void SetVertices(ReadOnlySpan<Vector3> vertices)
        {
            if (positions == null || positions.Length < vertices.Length)
                positions = new Vector3[vertices.Length];
                    
            verticesCount = vertices.Length;
            vertices.CopyTo(positions.AsSpan());
            
            if (!managedOnly)
            {
                if (verticesCount < 50)
                {
                    Span<UniversalVertex> temp = stackalloc UniversalVertex[verticesCount];
                    for (int i = 0; i < verticesCount; ++i)
                        temp[i] = new UniversalVertex() { position = vertices[i] };
                    VerticesBuffer?.UpdateBuffer(temp);
                }
                else
                {
                    var temp = new UniversalVertex[verticesCount];
                    for (int i = 0; i < verticesCount; ++i)
                        temp[i] = new UniversalVertex() { position = vertices[i] };
                    VerticesBuffer?.UpdateBuffer(temp.AsSpan(0, verticesCount));
                }
            }
        }
        
        // no alloc method
        public void SetVertices(Vector3 v1, Vector3 v2)
        {
            if (positions == null || positions.Length < 2)
                positions = new Vector3[2];
                    
            verticesCount = 2;
            positions[0] = v1;
            positions[1] = v2;
            
            if (!managedOnly)
            {
                Span<UniversalVertex> temp = stackalloc UniversalVertex[2];
                temp[0] = new UniversalVertex() { position = v1 };
                temp[1] = new UniversalVertex() { position = v2 };
                VerticesBuffer?.UpdateBuffer(temp);
            }
        }

        public void BuildBoundingBox()
        {
            BuildBoundingBox(positions.AsSpan(0, verticesCount));
        }

        private void BuildBoundingBox(ReadOnlySpan<Vector3> vertices)
        {
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            for (var index = 0; index < vertices.Length; index++)
            {
                var v = vertices[index];
                min.X = Math.Min(v.X, min.X);
                min.Y = Math.Min(v.Y, min.Y);
                min.Z = Math.Min(v.Z, min.Z);

                max.X = Math.Max(v.X, max.X);
                max.Y = Math.Max(v.Y, max.Y);
                max.Z = Math.Max(v.Z, max.Z);
            }

            bounds = new BoundingBox(min, max);
        }
        
        public void RebuildIndices()
        {
            if (managedOnly)
                throw new Exception("You may call rebuild only on unmanaged meshes");
            Debug.Assert(!managedOnly);
            Debug.Assert(positions != null);
            Debug.Assert(shortIndices != null || bigIndices != null);
            BuildBoundingBox();
            if (shortIndices != null)
               IndicesBuffer?.UpdateBuffer(MemoryMarshal.AsBytes(shortIndices.AsSpan(0, indicesCount)));
            else
                IndicesBuffer?.UpdateBuffer(MemoryMarshal.AsBytes(bigIndices.AsSpan(0, indicesCount)));
            //vertices = null;
            //indices = null;
        }

        private bool disposed;

        public void Dispose()
        {
            disposed = true;
            if (!IsManagedOnly)
            {
                VerticesBuffer?.Dispose();
                IndicesBuffer?.Dispose();
            }
            
            VerticesBuffer = null;
            IndicesBuffer = null;
        }
    }
}
