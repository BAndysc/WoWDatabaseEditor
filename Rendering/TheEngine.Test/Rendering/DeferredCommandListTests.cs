using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Rendering;
using TheMaths;
using Veldrid;
using Pipeline = TheEngine.Resources.Pipeline;

namespace TheEngine.Test.Rendering
{
    public class DeferredCommandListTests
    {
        private class FakeTexture : ITexture
        {
            public TextureHandle Handle => default;
            public int Width => 4;
            public int Height => 4;
        }

        private class FakeBuffer : INativeBuffer
        {
            public void Dispose() { }
            public void Activate(int slot) { }
        }

        private class FakeMesh : IMesh
        {
            public void SetIndices(ReadOnlySpan<ushort> indices, int submesh) => throw new NotImplementedException();
            public void SetSubmeshIndicesRange(int submesh, int start, int length) => throw new NotImplementedException();
            public void RebuildIndices() => throw new NotImplementedException();
            public void Activate() => throw new NotImplementedException();
            public void SetSubmeshCount(int count) => throw new NotImplementedException();
            public int IndexCount(int submesh) => 0;
            public int IndexStart(int submesh) => 0;
            public int SubmeshCount => 1;
            public BoundingBox Bounds => default;
            public MeshHandle Handle => default;
            public IndexType IndexType => IndexType.Short;
            public IEnumerable<(Vector4, Vector4, Vector4)> GetFaces(int submesh) => throw new NotImplementedException();
            public void SaveToObj(string path) => throw new NotImplementedException();
        }

        /// <summary>Captures every replayed command with its arguments, for sequence assertions.</summary>
        private class CapturingCommandList : ICommandList
        {
            public readonly List<(string op, object?[] args)> Calls = new();
            public readonly FakeBuffer TransientBuffer = new();

            private void Log(string op, params object?[] args) => Calls.Add((op, args));

            public void Dispose() { }
            public void Begin() => Log("Begin");
            public void End() => Log("End");
            public void BeginRenderingPass(in RenderPassDescriptor descriptor)
                => Log("BeginRenderingPass", descriptor.Target, descriptor.DefaultFramebuffer, descriptor.Width, descriptor.Height, descriptor.ColorLoadOp, descriptor.ClearColor, descriptor.ViewportScale);
            public void EndRenderingPass() => Log("EndRenderingPass");
            public bool InRenderingPass => false;
            public void Blit(ITexture source, ITexture destination, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, BlitMask mask, BlitFilter filter)
                => Log("Blit", source, destination, srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
            public void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to) => Log("Barrier", texture, from, to);
            public void SetPipeline(Pipeline pipeline, IShaderPass shaderPass) => Log("SetPipeline", pipeline, shaderPass);
            public void SetScissor(int x, int y, int width, int height) => Log("SetScissor", x, y, width, height);
            public void BindMaterialResources(Material material, MaterialInstanceRenderData? instanceData = null) => Log("BindMaterialResources", material, instanceData);
            public void BindMaterialResources(Material material, MaterialSnapshot snapshot) => Log("BindMaterialResourcesSnapshot", material, snapshot);
            public void BindUniformBuffer(int slot, INativeBuffer buffer) => Log("BindUniformBuffer", slot, buffer);
            public void BindTransientUniformBuffer<T>(int slot, ref T data) where T : unmanaged
                => BindTransientUniformBuffer(slot, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1)));
            public void BindTransientUniformBuffer(int slot, ReadOnlySpan<byte> data) => Log("BindTransientUniformBuffer", slot, data.ToArray());
            public INativeBuffer UploadTransientBuffer<T>(BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged
            {
                Log("UploadTransientBuffer", format, data.Length);
                return TransientBuffer;
            }
            public INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged
            {
                Log("UploadTransientBuffer", type, data.Length);
                return TransientBuffer;
            }
            public void BindMesh(IMesh? mesh) => Log("BindMesh", mesh);
            public void BindVertexBuffer(INativeBuffer buffer) => Log("BindVertexBuffer", buffer);
            public void BindIndexBuffer(INativeBuffer buffer, IndexType indexType) => Log("BindIndexBuffer", buffer, indexType);
            public void Draw(int vertexCount, int firstVertex) => Log("Draw", vertexCount, firstVertex);
            public void DrawIndexed(int indexCount, int firstIndex, int baseVertex) => Log("DrawIndexed", indexCount, firstIndex, baseVertex);
            public void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance)
                => Log("DrawIndexedInstanced", indexCount, instanceCount, firstIndex, baseVertex, firstInstance);
            public void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination) => Log("ReadPixels", texture);
            public void InsertDebugMarker(string label) => Log("InsertDebugMarker", label);
            public void CheckError(string context) => Log("CheckError", context);
            public void ValidateState() => Log("ValidateState");
            public int ShaderSwitches => 0;
            public int MeshSwitches => 0;
        }

        /// <summary>Allocation-free executor for the steady-state allocation test.</summary>
        private class NoopCommandList : ICommandList
        {
            private readonly FakeBuffer buffer = new();
            public void Dispose() { }
            public void Begin() { }
            public void End() { }
            public void BeginRenderingPass(in RenderPassDescriptor descriptor) { }
            public void EndRenderingPass() { }
            public bool InRenderingPass => false;
            public void Blit(ITexture source, ITexture destination, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, BlitMask mask, BlitFilter filter) { }
            public void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to) { }
            public void SetPipeline(Pipeline pipeline, IShaderPass shaderPass) { }
            public void SetScissor(int x, int y, int width, int height) { }
            public void BindMaterialResources(Material material, MaterialInstanceRenderData? instanceData = null) { }
            public void BindMaterialResources(Material material, MaterialSnapshot snapshot) { }
            public void BindUniformBuffer(int slot, INativeBuffer buffer) { }
            public void BindTransientUniformBuffer<T>(int slot, ref T data) where T : unmanaged { }
            public void BindTransientUniformBuffer(int slot, ReadOnlySpan<byte> data) { }
            public INativeBuffer UploadTransientBuffer<T>(BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged => buffer;
            public INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged => buffer;
            public void BindMesh(IMesh? mesh) { }
            public void BindVertexBuffer(INativeBuffer buffer) { }
            public void BindIndexBuffer(INativeBuffer buffer, IndexType indexType) { }
            public void Draw(int vertexCount, int firstVertex) { }
            public void DrawIndexed(int indexCount, int firstIndex, int baseVertex) { }
            public void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance) { }
            public void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination) { }
            public void InsertDebugMarker(string label) { }
            public void CheckError(string context) { }
            public void ValidateState() { }
            public int ShaderSwitches => 0;
            public int MeshSwitches => 0;
        }

        private static Pipeline MakePipeline(int handle)
        {
            var description = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleDisabled,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = RasterizerStateDescription.Default,
            };
            return new Pipeline(new PipelineHandle(handle), IRenderManager.DefaultOutput, default, null!, PrimitiveTopology.TriangleList, description, false);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TestUniform
        {
            public float A, B, C, D;
        }

        [Test]
        public void RecordedCommandsReplayInOrderWithSameArguments()
        {
            var fake = new CapturingCommandList();
            var deferred = new DeferredCommandList(fake);
            var pipeline = MakePipeline(1);
            var texture = new FakeTexture();
            var mesh = new FakeMesh();
            var buffer = new FakeBuffer();

            deferred.Begin();
            deferred.BeginRenderingPass(new RenderPassDescriptor { Target = texture, ColorLoadOp = LoadOp.Clear, ClearColor = new Color4(1, 0, 0, 1) });
            deferred.SetPipeline(pipeline, null!);
            deferred.SetScissor(1, 2, 3, 4);
            deferred.BindUniformBuffer(7, buffer);
            deferred.BindMesh(mesh);
            deferred.DrawIndexed(60, 3, 0);
            deferred.EndRenderingPass();
            deferred.Barrier(texture, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
            deferred.InsertDebugMarker("marker");
            deferred.End();

            var calls = fake.Calls;
            int i = 0;
            Assert.AreEqual("Begin", calls[i++].op);
            Assert.AreEqual("BeginRenderingPass", calls[i].op);
            Assert.AreSame(texture, calls[i].args[0]);
            Assert.AreEqual(LoadOp.Clear, calls[i].args[4]);
            Assert.AreEqual(new Color4(1, 0, 0, 1), calls[i++].args[5]);
            Assert.AreEqual("SetPipeline", calls[i].op);
            Assert.AreSame(pipeline, calls[i++].args[0]);
            Assert.AreEqual("SetScissor", calls[i].op);
            Assert.AreEqual(new object[] { 1, 2, 3, 4 }, calls[i++].args);
            Assert.AreEqual("BindUniformBuffer", calls[i].op);
            Assert.AreEqual(7, calls[i].args[0]);
            Assert.AreSame(buffer, calls[i++].args[1]);
            Assert.AreEqual("BindMesh", calls[i].op);
            Assert.AreSame(mesh, calls[i++].args[0]);
            Assert.AreEqual("DrawIndexed", calls[i].op);
            Assert.AreEqual(new object[] { 60, 3, 0 }, calls[i++].args);
            Assert.AreEqual("EndRenderingPass", calls[i++].op);
            Assert.AreEqual("Barrier", calls[i].op);
            Assert.AreSame(texture, calls[i++].args[0]);
            Assert.AreEqual("InsertDebugMarker", calls[i].op);
            Assert.AreEqual("marker", calls[i++].args[0]);
            Assert.AreEqual("End", calls[i++].op);
            Assert.AreEqual(i, calls.Count);
        }

        [Test]
        public void TransientUniformDataIsSnapshottedAtRecordTime()
        {
            var fake = new CapturingCommandList();
            var deferred = new DeferredCommandList(fake);

            var data = new TestUniform { A = 1 };
            deferred.Begin();
            deferred.BindTransientUniformBuffer(0, ref data);
            data.A = 2; // mutated after recording - the replay must still see A = 1 for the first bind
            deferred.BindTransientUniformBuffer(0, ref data);
            deferred.End();

            var binds = fake.Calls.FindAll(c => c.op == "BindTransientUniformBuffer");
            Assert.AreEqual(2, binds.Count);
            var first = MemoryMarshal.Read<TestUniform>((byte[])binds[0].args[1]!);
            var second = MemoryMarshal.Read<TestUniform>((byte[])binds[1].args[1]!);
            Assert.AreEqual(1f, first.A);
            Assert.AreEqual(2f, second.A);
        }

        [Test]
        public void TransientUploadsExecuteAtRecordTimeAndReturnTheRealBuffer()
        {
            var fake = new CapturingCommandList();
            var deferred = new DeferredCommandList(fake);

            deferred.Begin();
            var returned = deferred.UploadTransientBuffer(BufferInternalFormat.Float4, (ReadOnlySpan<Vector4>)new Vector4[3]);
            // the upload must have reached the executor already, before any replay
            Assert.AreSame(fake.TransientBuffer, returned);
            Assert.AreEqual("UploadTransientBuffer", fake.Calls[^1].op);
            deferred.End();
        }

        [Test]
        public void RedundantPipelineAndMeshBindsAreNotRecorded()
        {
            var fake = new CapturingCommandList();
            var deferred = new DeferredCommandList(fake);
            var pipeline = MakePipeline(1);
            var mesh = new FakeMesh();

            deferred.Begin();
            deferred.BeginRenderingPass(new RenderPassDescriptor());
            deferred.SetPipeline(pipeline, null!);
            deferred.BindMesh(mesh);
            deferred.DrawIndexed(3, 0, 0);
            deferred.SetPipeline(pipeline, null!); // same pipeline + pass - must be skipped
            deferred.BindMesh(mesh);               // same mesh - must be skipped
            deferred.DrawIndexed(3, 3, 0);
            deferred.EndRenderingPass();
            deferred.End();

            Assert.AreEqual(1, fake.Calls.FindAll(c => c.op == "SetPipeline").Count);
            Assert.AreEqual(1, fake.Calls.FindAll(c => c.op == "BindMesh").Count);
            Assert.AreEqual(2, fake.Calls.FindAll(c => c.op == "DrawIndexed").Count);
        }

        [Test]
        public void DirectBufferBindsInvalidateTheMeshDeduplication()
        {
            var fake = new CapturingCommandList();
            var deferred = new DeferredCommandList(fake);
            var mesh = new FakeMesh();
            var buffer = new FakeBuffer();

            deferred.Begin();
            deferred.BindMesh(mesh);
            deferred.BindVertexBuffer(buffer); // bypasses the mesh - the next BindMesh must be recorded again
            deferred.BindMesh(mesh);
            deferred.End();

            Assert.AreEqual(2, fake.Calls.FindAll(c => c.op == "BindMesh").Count);
        }

        [Test]
        public void SteadyStateRecordingAndReplayAllocatesNothing()
        {
            var deferred = new DeferredCommandList(new NoopCommandList());
            var pipeline = MakePipeline(1);
            var mesh = new FakeMesh();
            var data = new TestUniform { A = 1 };

            void Frame()
            {
                deferred.Begin();
                deferred.BeginRenderingPass(new RenderPassDescriptor { ColorLoadOp = LoadOp.Clear });
                deferred.SetPipeline(pipeline, null!);
                for (int i = 0; i < 1000; ++i)
                {
                    deferred.BindTransientUniformBuffer(1, ref data);
                    deferred.BindMesh(mesh);
                    deferred.DrawIndexed(30, 0, 0);
                }
                deferred.InsertDebugMarker("frame");
                deferred.EndRenderingPass();
                deferred.CheckError("end");
                deferred.End();
            }

            // warm up: grow the stream/ref arrays to their high-water mark
            Frame();
            Frame();

            long before = GC.GetAllocatedBytesForCurrentThread();
            Frame();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, allocated, "recording + replay of a steady-state frame must not allocate");
        }

        [Test]
        public void ReadPixelsPassesThroughToTheExecutorImmediately()
        {
            var fake = new CapturingCommandList();
            var deferred = new DeferredCommandList(fake);
            var texture = new FakeTexture();
            Span<uint> pixel = stackalloc uint[1];

            deferred.Begin();
            deferred.End();
            deferred.ReadPixels(texture, 1, 0, 0, 1, 1, pixel);

            Assert.AreEqual("ReadPixels", fake.Calls[^1].op);
        }
    }
}
