using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenGLBindings;
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

        internal int VertexArrayObject { get; }
        internal NativeBuffer<UniversalVertex>? VerticesBuffer { get; private set; }
        internal NativeBuffer<ushort>? IndicesBuffer { get; private set; }

        private BoundingBox bounds;
        private UniversalVertex[]? vertices;
        private ushort[]? indices;
        private int indicesCount = 0;
        private (int start, int length)[]? submeshesRange;

        public BoundingBox Bounds => bounds;
        
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
            Debug.Assert(vertices != null);
            Debug.Assert(indices != null);
            int start = IndexStart(submesh);
            int count = IndexCount(submesh);
            for (int i = start; i + 2 < start + count; i += 3)
            {
                ushort j = indices[i];
                ushort k = indices[i+1];
                ushort l = indices[i+2];
                
                if (vertices.Length > j &&
                    vertices.Length > k &&
                    vertices.Length > l)
                {
                    var v1 = vertices[j].position;
                    var v2 = vertices[k].position;
                    var v3 = vertices[l].position;
                    yield return (v1, v2, v3);
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
                IndicesBuffer = engine.Device.CreateBuffer<ushort>(BufferTypeEnum.Index, 1);
                VertexArrayObject = engine.Device.device.GenVertexArray();
                engine.Device.device.BindVertexArray(VertexArrayObject);
                VerticesBuffer.Activate(0);
                IndicesBuffer.Activate(0);
                int location = 0;
                int accumulatedSize = 0;
                int stride = 4 * 4 * 3 + 2 * 4 * 2;
                engine.Device.device.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(0));
                engine.Device.device.EnableVertexAttribArray(0);
                
                engine.Device.device.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(16));
                engine.Device.device.EnableVertexAttribArray(1);
                
                engine.Device.device.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(32));
                engine.Device.device.EnableVertexAttribArray(2);
                
                engine.Device.device.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(48));
                engine.Device.device.EnableVertexAttribArray(3);
                
                engine.Device.device.VertexAttribPointer(4, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(56));
                engine.Device.device.EnableVertexAttribArray(4);
                engine.Device.device.BindVertexArray(0);
            }
        }

        internal Mesh(Engine engine, MeshHandle handle, UniversalVertex[] vertices, ushort[] indices, int indicesCount, bool ownVerticesArray, bool managedOnly)
        {
            this.engine = engine;
            Handle = handle;
            this.vertices = ownVerticesArray ? vertices : vertices.ToArray();
            this.indices = indices;
            this.indicesCount = indicesCount;
            this.managedOnly = managedOnly;
            BuildBoundingBox(vertices);
            if (!managedOnly)
            {
                VerticesBuffer = engine.Device.CreateBuffer<UniversalVertex>(BufferTypeEnum.Vertex, vertices);
                IndicesBuffer = engine.Device.CreateBuffer<ushort>(BufferTypeEnum.Index, indices.AsSpan(0, indicesCount));
                VertexArrayObject = engine.Device.device.GenVertexArray();
                engine.Device.device.BindVertexArray(VertexArrayObject);
                VerticesBuffer.Activate(0);
                IndicesBuffer.Activate(0);
                int location = 0;
                int accumulatedSize = 0;
                int stride = 4 * 4 * 3 + 2 * 4 * 2;
                engine.Device.device.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(0));
                engine.Device.device.EnableVertexAttribArray(0);
            
                engine.Device.device.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(16));
                engine.Device.device.EnableVertexAttribArray(1);
            
                engine.Device.device.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, new IntPtr(32));
                engine.Device.device.EnableVertexAttribArray(2);
            
                engine.Device.device.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(48));
                engine.Device.device.EnableVertexAttribArray(3);
            
                engine.Device.device.VertexAttribPointer(4, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(56));
                engine.Device.device.EnableVertexAttribArray(4);
                engine.Device.device.BindVertexArray(0);
            }
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
            Debug.Assert(indices.Length >= start + length);
            if (submeshesRange == null && submesh == 0)
            {
                if (start == 0 && length == indices.Length)
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
            if (submeshesRange == null && submesh == 0)
            {
                this.indices = indices.ToArray();
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
                    Array.Copy(this.indices, newIndices, indicesCount);
                    indices.CopyTo(newIndices.AsSpan(indicesCount, indices.Length));
                    //Array.Copy(indices, 0, newIndices, this.indices.Length, indices.Length);
                    submeshesRange[submesh] = (indicesCount, indices.Length);
                    this.indices = newIndices;
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
            this.vertices = new UniversalVertex[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
                this.vertices[i] = new UniversalVertex() { position = new Vector4(vertices[i], 1) };
        }

        public void BuildBoundingBox() => BuildBoundingBox(vertices);

        private void BuildBoundingBox(UniversalVertex[] vertices)
        {
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            foreach (var v in vertices)
            {
                min.X = Math.Min(v.position.X, min.X);
                min.Y = Math.Min(v.position.Y, min.Y);
                min.Z = Math.Min(v.position.Z, min.Z);
                
                max.X = Math.Max(v.position.X, max.X);
                max.Y = Math.Max(v.position.Y, max.Y);
                max.Z = Math.Max(v.position.Z, max.Z);
            }

            bounds = new BoundingBox(min, max);
        }
        
        public void Rebuild()
        {
            Debug.Assert(!managedOnly);
            Debug.Assert(vertices != null);
            Debug.Assert(indices != null);
            BuildBoundingBox(vertices);
            VerticesBuffer.UpdateBuffer(vertices);
            IndicesBuffer.UpdateBuffer(indices);
            //vertices = null;
            //indices = null;
        }

        private bool disposed;

        public void Dispose()
        {
            disposed = true;
            if (!IsManagedOnly)
            {
                VerticesBuffer.Dispose();
                IndicesBuffer.Dispose();
            }
            
            VerticesBuffer = null;
            IndicesBuffer = null;
        }
    }
}
