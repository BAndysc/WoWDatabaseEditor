using System;
using System.Runtime.InteropServices;

namespace ProtoZeroSharp;

public unsafe struct ProtoReader
{
    private byte* memory;

    private int length;

    private int currentTag;

    private int offset;

    private int currentField;

    private ProtoWireType currentWireType;

    public int Remaining => length - offset;

    public ProtoReader(byte* memory, int length)
    {
        this.memory = memory;
        this.length = length;
        offset = 0;
        currentTag = 0;
        currentField = 0;
        currentWireType = ProtoWireType.VarInt;
    }

    private Span<byte> GetSpan(int maxLength = 0)
    {
        var leftLength = length - offset;
        return new Span<byte>(memory + offset, maxLength == 0 ? leftLength : Math.Min(maxLength, leftLength));
    }

    private void MoveForward(int length)
    {
        offset += length;
    }

    public bool Next()
    {
        var tagSpan = GetSpan(VarInt.MaxBytesCount);
        if (tagSpan.Length == 0)
            return false;

        MoveForward(ProtobufFormat.ReadFieldHeader(tagSpan, out currentField, out currentWireType));
        return true;
    }

    public int Tag => currentField;

    public ulong ReadVarInt()
    {
        if ((memory[offset] & 0x80) == 0)
        {
            offset++;
            return memory[offset - 1];
        }
        MoveForward(VarInt.ReadVarint(GetSpan(), out var value));
        return value;
    }

    public float ReadFloat()
    {
        var span = GetSpan(4);
        MoveForward(4);
        return MemoryMarshal.Read<float>(span);
    }

    public double ReadDouble()
    {
        var span = GetSpan(8);
        MoveForward(8);
        return MemoryMarshal.Read<double>(span);
    }

    public bool ReadBool()
    {
        return ReadVarInt() != 0;
    }

    public Span<byte> ReadBytes()
    {
        MoveForward(VarInt.ReadVarint(GetSpan(), out var len));
        var span = GetSpan((int)len);
        MoveForward((int)len);
        return span;
    }

    public ProtoReader ReadMessage()
    {
        MoveForward(VarInt.ReadVarint(GetSpan(), out var len));
        var reader = new ProtoReader(memory + offset, (int)len);
        MoveForward((int)len);
        return reader;
    }

    public string ReadString()
    {
        var bytes = ReadBytes();
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public void Skip()
    {
        switch (currentWireType)
        {
            case ProtoWireType.VarInt:
                MoveForward(VarInt.ReadVarint(GetSpan(), out _));
                break;
            case ProtoWireType.Fixed64:
                MoveForward(8);
                break;
            case ProtoWireType.Length:
                MoveForward(VarInt.ReadVarint(GetSpan(), out var len));
                MoveForward((int)len);
                break;
            case ProtoWireType.StartGroup:
                throw new ArgumentOutOfRangeException();
                break;
            case ProtoWireType.EndGroup:
                throw new ArgumentOutOfRangeException();
                break;
            case ProtoWireType.Fixed32:
                MoveForward(4);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}