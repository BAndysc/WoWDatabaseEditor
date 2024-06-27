using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ProtoZeroSharp;

public unsafe struct ProtoWriter
{
    private readonly ArenaAllocator* memory;
    private StackArray<ArenaAllocator.ChunkOffset> submessagesStack;
    private StackArray<int> lengthsStack;
    private bool ownsMemory;
    private int currentMessageLength;

    public ProtoWriter() : this((ArenaAllocator*)Marshal.AllocHGlobal(sizeof(ArenaAllocator)))
    {
        ownsMemory = true;
        *memory = new ArenaAllocator();
    }

    private ProtoWriter(ArenaAllocator* memory)
    {
        this.memory = memory;
        currentMessageLength = 0;
        ownsMemory = false;
        submessagesStack = new StackArray<ArenaAllocator.ChunkOffset>();
        lengthsStack = new StackArray<int>();
    }

    public int GetTotalLength() => memory->GetTotalLength();

    public void Free()
    {
        if (ownsMemory)
        {
            memory->Free();
            Marshal.FreeHGlobal((IntPtr)memory);
        }
    }

    public int CopyTo(Span<byte> span)
    {
        return memory->CopyTo(span);
    }

    public void WriteTo(Stream stream)
    {
        memory->WriteTo(stream);
    }

    public void AddVarInt(int messageId, long value) => AddVarInt(messageId, (ulong)value);

    public void AddVarInt(int messageId, ulong value)
    {
        var span = memory->ReserveContiguousSpan(ProtobufFormat.VarIntFieldLenUpperBound);
        int written = ProtobufFormat.WriteVarIntField(span, messageId, value);
        memory->MoveForward(written);
        currentMessageLength += written;
    }

    public void AddBytes(int messageId, ReadOnlySpan<byte> payload)
    {
        // this could be optimized to write in chunks, but needs a better API
        var span = memory->ReserveContiguousSpan(ProtobufFormat.BytesFieldLenUpperBound(payload.Length));
        int written = ProtobufFormat.WriteBytes(span, messageId, payload);
        memory->MoveForward(written);
        currentMessageLength += written;
    }

    public void AddString(int messageId, string payload)
    {
        var payloadLength = Encoding.UTF8.GetByteCount(payload);
        // this could be optimized to write in chunks, but needs a better API
        var span = memory->ReserveContiguousSpan(ProtobufFormat.BytesFieldLenUpperBound(payloadLength));
        int written = ProtobufFormat.WriteString(span, messageId, payloadLength, payload);
        memory->MoveForward(written);
        currentMessageLength += written;
    }

    public void StartSub(int messageId)
    {
        var headerSpan = memory->ReserveContiguousSpan(ProtobufFormat.SubMessageHeaderLenUpperBound);
        int written = ProtobufFormat.WriteLengthFieldHeader(headerSpan, messageId);
        currentMessageLength += written;
        memory->MoveForward(written);
        submessagesStack.Add(memory->Position);
        lengthsStack.Add(currentMessageLength);
        memory->MoveForward(5); // reserve space for the length
        currentMessageLength = 0;
    }

    public void CloseSub(bool optimizeSizeOverPerformance = true)
    {
        var lastSubMessageStart = submessagesStack.PeekAndPop();

        var lengthSpan = lastSubMessageStart.Chunk->GetSpan(lastSubMessageStart.Offset);
        int written;
        if (optimizeSizeOverPerformance)
        {
            written = ProtobufFormat.WriteLengthFieldLength(lengthSpan, currentMessageLength);
            if (written < 5)
            {
                lastSubMessageStart.Chunk->Erase(lastSubMessageStart.Offset + written, 5 - written);
            }
        }
        else
        {
            written = ProtobufFormat.WriteLengthFieldLength(lengthSpan, currentMessageLength, 5);
            Debug.Assert(written == 5);
        }

        currentMessageLength = lengthsStack.PeekAndPop() + written + currentMessageLength;
    }
}
