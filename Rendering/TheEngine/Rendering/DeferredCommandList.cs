using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using Pipeline = TheEngine.Resources.Pipeline;

namespace TheEngine.Rendering;

/// <summary>
/// Records every command into a flat byte stream ([1 byte opcode][unmanaged payload]) and
/// replays it into the executor on <see cref="End"/> - the GL rehearsal of a Vulkan command
/// buffer, proving that nothing in the engine depends on commands executing immediately.
///
/// Zero-allocation by design: payloads are unmanaged structs written with
/// <see cref="Unsafe.WriteUnaligned{T}(ref byte, T)"/>. Managed references (pipelines,
/// meshes, textures, buffers, labels) cannot live inside the byte stream - the GC can't
/// trace pointers stored in raw bytes - so they go into a parallel, reused object table
/// and the payload stores the table index. Every array and pool grows to a high-water
/// mark and is reused across frames.
///
/// Record-time vs replay-time semantics:
///  - transient buffer uploads execute at record time and return a real buffer (resource
///    writes are not commands - the same way a Vulkan ring buffer is written while
///    recording); only binds become commands,
///  - transient uniform data (SceneData/ObjectData) is copied inline into the stream,
///  - material state is captured into a pooled <see cref="MaterialSnapshot"/>, because
///    materials are mutated between draws and their live state at replay would be wrong,
///  - <see cref="ReadPixels"/> passes through to the executor immediately: it is the
///    synchronous picking readback, called between frames when nothing is being recorded,
///    and it reads the previously executed frame either way.
/// </summary>
internal sealed class DeferredCommandList : ICommandList
{
    private enum Op : byte
    {
        BeginRenderingPass,
        EndRenderingPass,
        Blit,
        Barrier,
        SetPipeline,
        SetScissor,
        BindMaterialResources,
        BindUniformBuffer,
        BindTransientUniformBuffer,
        BindMesh,
        BindVertexBuffer,
        BindIndexBuffer,
        Draw,
        DrawIndexed,
        DrawIndexedInstanced,
        InsertDebugMarker,
        CheckError,
        ValidateState,
    }

    private struct BeginPassPayload
    {
        public int TargetRef;
        public int DefaultFramebuffer;
        public int Width, Height;
        public byte ColorLoadOp;
        public Color4 ClearColor;
        public float ViewportScale;
    }

    private struct BlitPayload
    {
        public int SourceRef, DestinationRef;
        public int SrcX0, SrcY0, SrcX1, SrcY1;
        public int DstX0, DstY0, DstX1, DstY1;
        public byte Mask, Filter;
    }

    private struct BarrierPayload
    {
        public int TextureRef;
        public byte From, To;
    }

    private struct SetPipelinePayload
    {
        public int PipelineRef, ShaderPassRef;
    }

    private struct ScissorPayload
    {
        public int X, Y, Width, Height;
    }

    private struct BindMaterialPayload
    {
        public int MaterialRef, SnapshotRef;
    }

    private struct BindUniformBufferPayload
    {
        public int Slot, BufferRef;
    }

    private struct BindTransientUniformPayload
    {
        public int Slot, Length; // followed by Length data bytes inline in the stream
    }

    private struct BindIndexBufferPayload
    {
        public int BufferRef;
        public byte IndexType;
    }

    private struct DrawPayload
    {
        public int VertexCount, FirstVertex;
    }

    private struct DrawIndexedPayload
    {
        public int IndexCount, FirstIndex, BaseVertex;
    }

    private struct DrawIndexedInstancedPayload
    {
        public int IndexCount, InstanceCount, FirstIndex, BaseVertex, FirstInstance;
    }

    private readonly ICommandList executor;

    private byte[] stream = new byte[256 * 1024];
    private int streamLength;
    private object?[] refs = new object?[4096];
    private int refCount;
    private readonly List<MaterialSnapshot> snapshotPool = new();
    private int snapshotsUsed;

    private bool recording;
    private bool inPass;

    // record-side mirrors of the executor's binding caches: EngineCommandList re-binds the
    // mesh/pipeline on every draw and relies on the command list to deduplicate, so the
    // deduplication has to happen at record time or the stream fills up with no-ops
    private PipelineHandle lastPipeline = PipelineHandle.Empty;
    private IShaderPass? lastShaderPass;
    private IMesh? lastMesh;
    private bool meshBindingValid;

    public DeferredCommandList(ICommandList executor)
    {
        this.executor = executor;
    }

    public void Dispose()
    {
        // owns no GPU resources; the executor is owned and disposed by its creator
    }

    public bool InRenderingPass => inPass;

    // only meaningful after End (the executor counts them during the replay)
    public int ShaderSwitches => executor.ShaderSwitches;
    public int MeshSwitches => executor.MeshSwitches;

    public void Begin()
    {
        Debug.Assert(!recording, "Begin called while already recording");
        executor.Begin();
        streamLength = 0;
        refCount = 0;
        snapshotsUsed = 0;
        recording = true;
        inPass = false;
        lastPipeline = PipelineHandle.Empty;
        lastShaderPass = null;
        lastMesh = null;
        meshBindingValid = false;
    }

    public void End()
    {
        Debug.Assert(recording, "End called without Begin");
        Debug.Assert(!inPass, "End called inside a rendering pass");
        recording = false;
        Replay(executor);
        executor.End();
        // drop the recorded references so disposed resources don't stay alive a frame longer
        Array.Clear(refs, 0, refCount);
        for (int i = 0; i < snapshotsUsed; ++i)
            snapshotPool[i].Clear();
    }

    public void BeginRenderingPass(in RenderPassDescriptor descriptor)
    {
        Debug.Assert(!inPass, "BeginRenderingPass called while another pass is active");
        inPass = true;
        WriteOp(Op.BeginRenderingPass);
        Write(new BeginPassPayload
        {
            TargetRef = AddRef(descriptor.Target),
            DefaultFramebuffer = descriptor.DefaultFramebuffer,
            Width = descriptor.Width,
            Height = descriptor.Height,
            ColorLoadOp = (byte)descriptor.ColorLoadOp,
            ClearColor = descriptor.ClearColor,
            ViewportScale = descriptor.ViewportScale,
        });
    }

    public void EndRenderingPass()
    {
        Debug.Assert(inPass, "EndRenderingPass called without an active pass");
        inPass = false;
        WriteOp(Op.EndRenderingPass);
    }

    public void Blit(ITexture source, ITexture destination, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, BlitMask mask, BlitFilter filter)
    {
        Debug.Assert(!inPass, "Blit is only legal outside a rendering pass");
        WriteOp(Op.Blit);
        Write(new BlitPayload
        {
            SourceRef = AddRef(source),
            DestinationRef = AddRef(destination),
            SrcX0 = srcX0, SrcY0 = srcY0, SrcX1 = srcX1, SrcY1 = srcY1,
            DstX0 = dstX0, DstY0 = dstY0, DstX1 = dstX1, DstY1 = dstY1,
            Mask = (byte)mask,
            Filter = (byte)filter,
        });
    }

    public void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to)
    {
        Debug.Assert(!inPass, "Barrier is only legal outside a rendering pass");
        WriteOp(Op.Barrier);
        Write(new BarrierPayload { TextureRef = AddRef(texture), From = (byte)from, To = (byte)to });
    }

    public void SetPipeline(Pipeline pipeline, IShaderPass shaderPass)
    {
        if (lastPipeline == pipeline.Handle && lastShaderPass == shaderPass)
            return;
        lastPipeline = pipeline.Handle;
        lastShaderPass = shaderPass;
        WriteOp(Op.SetPipeline);
        Write(new SetPipelinePayload { PipelineRef = AddRef(pipeline), ShaderPassRef = AddRef(shaderPass) });
    }

    public void SetScissor(int x, int y, int width, int height)
    {
        WriteOp(Op.SetScissor);
        Write(new ScissorPayload { X = x, Y = y, Width = width, Height = height });
    }

    public void BindMaterialResources(Material material, MaterialInstanceRenderData? instanceData = null)
    {
        Debug.Assert(lastShaderPass != null, "BindMaterialResources requires a pipeline bound via SetPipeline");
        Debug.Assert(lastPipeline == material.Pipeline.Handle, "the bound pipeline doesn't belong to this material");
        // the material (and the instance data) are mutable and may change before the replay,
        // so everything their activation would read is captured now
        var snapshot = RentSnapshot();
        snapshot.CaptureFrom(material, lastShaderPass!, instanceData);
        WriteOp(Op.BindMaterialResources);
        Write(new BindMaterialPayload { MaterialRef = AddRef(material), SnapshotRef = AddRef(snapshot) });
    }

    public void BindMaterialResources(Material material, MaterialSnapshot snapshot)
    {
        Debug.Assert(lastShaderPass != null, "BindMaterialResources requires a pipeline bound via SetPipeline");
        WriteOp(Op.BindMaterialResources);
        Write(new BindMaterialPayload { MaterialRef = AddRef(material), SnapshotRef = AddRef(snapshot) });
    }

    public void BindUniformBuffer(int slot, INativeBuffer buffer)
    {
        WriteOp(Op.BindUniformBuffer);
        Write(new BindUniformBufferPayload { Slot = slot, BufferRef = AddRef(buffer) });
    }

    public void BindTransientUniformBuffer<T>(int slot, ref T data) where T : unmanaged
        => BindTransientUniformBuffer(slot, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1)));

    public void BindTransientUniformBuffer(int slot, ReadOnlySpan<byte> data)
    {
        WriteOp(Op.BindTransientUniformBuffer);
        Write(new BindTransientUniformPayload { Slot = slot, Length = data.Length });
        WriteBytes(data);
    }

    public INativeBuffer UploadTransientBuffer<T>(BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged
        => executor.UploadTransientBuffer(format, data); // resource write, not a command - executes now

    public INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged
        => executor.UploadTransientBuffer(type, data); // resource write, not a command - executes now

    public void BindMesh(IMesh? mesh)
    {
        if (meshBindingValid && lastMesh == mesh)
            return;
        lastMesh = mesh;
        meshBindingValid = true;
        WriteOp(Op.BindMesh);
        Write(AddRef(mesh));
    }

    public void BindVertexBuffer(INativeBuffer buffer)
    {
        // direct binds invalidate the mesh binding, mirroring the executor
        lastMesh = null;
        meshBindingValid = false;
        WriteOp(Op.BindVertexBuffer);
        Write(AddRef(buffer));
    }

    public void BindIndexBuffer(INativeBuffer buffer, IndexType indexType)
    {
        lastMesh = null;
        meshBindingValid = false;
        WriteOp(Op.BindIndexBuffer);
        Write(new BindIndexBufferPayload { BufferRef = AddRef(buffer), IndexType = (byte)indexType });
    }

    public void Draw(int vertexCount, int firstVertex)
    {
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        WriteOp(Op.Draw);
        Write(new DrawPayload { VertexCount = vertexCount, FirstVertex = firstVertex });
    }

    public void DrawIndexed(int indexCount, int firstIndex, int baseVertex)
    {
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        WriteOp(Op.DrawIndexed);
        Write(new DrawIndexedPayload { IndexCount = indexCount, FirstIndex = firstIndex, BaseVertex = baseVertex });
    }

    public void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance)
    {
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        WriteOp(Op.DrawIndexedInstanced);
        Write(new DrawIndexedInstancedPayload
        {
            IndexCount = indexCount,
            InstanceCount = instanceCount,
            FirstIndex = firstIndex,
            BaseVertex = baseVertex,
            FirstInstance = firstInstance,
        });
    }

    public void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination)
    {
        Debug.Assert(!inPass, "ReadPixels is only legal outside a rendering pass");
        executor.ReadPixels(texture, colorAttachment, x, y, width, height, destination);
    }

    public void InsertDebugMarker(string label)
    {
        WriteOp(Op.InsertDebugMarker);
        Write(AddRef(label));
    }

    public void CheckError(string context)
    {
        // a debug probe, not a frame command: during setup (resource creation in
        // constructors, between frames) nothing is recorded, so check the live context
        if (!recording)
        {
            executor.CheckError(context);
            return;
        }
        WriteOp(Op.CheckError);
        Write(AddRef(context));
    }

    public void ValidateState()
    {
#if DEBUG
        WriteOp(Op.ValidateState);
#endif
    }

    internal void Replay(ICommandList target)
    {
        int pos = 0;
        while (pos < streamLength)
        {
            var op = (Op)stream[pos++];
            switch (op)
            {
                case Op.BeginRenderingPass:
                {
                    var p = Read<BeginPassPayload>(ref pos);
                    target.BeginRenderingPass(new RenderPassDescriptor
                    {
                        Target = RefAt<ITexture>(p.TargetRef),
                        DefaultFramebuffer = p.DefaultFramebuffer,
                        Width = p.Width,
                        Height = p.Height,
                        ColorLoadOp = (LoadOp)p.ColorLoadOp,
                        ClearColor = p.ClearColor,
                        ViewportScale = p.ViewportScale,
                    });
                    break;
                }
                case Op.EndRenderingPass:
                    target.EndRenderingPass();
                    break;
                case Op.Blit:
                {
                    var p = Read<BlitPayload>(ref pos);
                    target.Blit(RefAt<ITexture>(p.SourceRef)!, RefAt<ITexture>(p.DestinationRef)!,
                        p.SrcX0, p.SrcY0, p.SrcX1, p.SrcY1, p.DstX0, p.DstY0, p.DstX1, p.DstY1,
                        (BlitMask)p.Mask, (BlitFilter)p.Filter);
                    break;
                }
                case Op.Barrier:
                {
                    var p = Read<BarrierPayload>(ref pos);
                    target.Barrier(RefAt<ITexture>(p.TextureRef)!, (ResourceUsage)p.From, (ResourceUsage)p.To);
                    break;
                }
                case Op.SetPipeline:
                {
                    var p = Read<SetPipelinePayload>(ref pos);
                    target.SetPipeline(RefAt<Pipeline>(p.PipelineRef)!, RefAt<IShaderPass>(p.ShaderPassRef)!);
                    break;
                }
                case Op.SetScissor:
                {
                    var p = Read<ScissorPayload>(ref pos);
                    target.SetScissor(p.X, p.Y, p.Width, p.Height);
                    break;
                }
                case Op.BindMaterialResources:
                {
                    var p = Read<BindMaterialPayload>(ref pos);
                    target.BindMaterialResources(RefAt<Material>(p.MaterialRef)!, RefAt<MaterialSnapshot>(p.SnapshotRef)!);
                    break;
                }
                case Op.BindUniformBuffer:
                {
                    var p = Read<BindUniformBufferPayload>(ref pos);
                    target.BindUniformBuffer(p.Slot, RefAt<INativeBuffer>(p.BufferRef)!);
                    break;
                }
                case Op.BindTransientUniformBuffer:
                {
                    var p = Read<BindTransientUniformPayload>(ref pos);
                    target.BindTransientUniformBuffer(p.Slot, stream.AsSpan(pos, p.Length));
                    pos += p.Length;
                    break;
                }
                case Op.BindMesh:
                    target.BindMesh(RefAt<IMesh>(Read<int>(ref pos)));
                    break;
                case Op.BindVertexBuffer:
                    target.BindVertexBuffer(RefAt<INativeBuffer>(Read<int>(ref pos))!);
                    break;
                case Op.BindIndexBuffer:
                {
                    var p = Read<BindIndexBufferPayload>(ref pos);
                    target.BindIndexBuffer(RefAt<INativeBuffer>(p.BufferRef)!, (IndexType)p.IndexType);
                    break;
                }
                case Op.Draw:
                {
                    var p = Read<DrawPayload>(ref pos);
                    target.Draw(p.VertexCount, p.FirstVertex);
                    break;
                }
                case Op.DrawIndexed:
                {
                    var p = Read<DrawIndexedPayload>(ref pos);
                    target.DrawIndexed(p.IndexCount, p.FirstIndex, p.BaseVertex);
                    break;
                }
                case Op.DrawIndexedInstanced:
                {
                    var p = Read<DrawIndexedInstancedPayload>(ref pos);
                    target.DrawIndexedInstanced(p.IndexCount, p.InstanceCount, p.FirstIndex, p.BaseVertex, p.FirstInstance);
                    break;
                }
                case Op.InsertDebugMarker:
                    target.InsertDebugMarker(RefAt<string>(Read<int>(ref pos))!);
                    break;
                case Op.CheckError:
                    target.CheckError(RefAt<string>(Read<int>(ref pos))!);
                    break;
                case Op.ValidateState:
                    target.ValidateState();
                    break;
                default:
                    throw new InvalidOperationException($"corrupted command stream: unknown opcode {op} at {pos - 1}");
            }
        }
    }

    /// <summary>Recorded stream size in bytes; debug/stats aid.</summary>
    internal int StreamLength => streamLength;

    private MaterialSnapshot RentSnapshot()
    {
        if (snapshotsUsed == snapshotPool.Count)
            snapshotPool.Add(new MaterialSnapshot());
        return snapshotPool[snapshotsUsed++];
    }

    private void WriteOp(Op op)
    {
        Debug.Assert(recording, "recording a command outside Begin/End");
        EnsureCapacity(1);
        stream[streamLength++] = (byte)op;
    }

    private void Write<T>(T payload) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        EnsureCapacity(size);
        Unsafe.WriteUnaligned(ref stream[streamLength], payload);
        streamLength += size;
    }

    private void WriteBytes(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(stream.AsSpan(streamLength));
        streamLength += data.Length;
    }

    private void EnsureCapacity(int size)
    {
        if (streamLength + size > stream.Length)
            Array.Resize(ref stream, Math.Max(stream.Length * 2, streamLength + size));
    }

    private T Read<T>(ref int pos) where T : unmanaged
    {
        var value = Unsafe.ReadUnaligned<T>(ref stream[pos]);
        pos += Unsafe.SizeOf<T>();
        return value;
    }

    private int AddRef(object? obj)
    {
        if (obj == null)
            return -1;
        if (refCount == refs.Length)
            Array.Resize(ref refs, refs.Length * 2);
        refs[refCount] = obj;
        return refCount++;
    }

    private T? RefAt<T>(int slot) where T : class
        => slot < 0 ? null : (T)refs[slot]!;
}
