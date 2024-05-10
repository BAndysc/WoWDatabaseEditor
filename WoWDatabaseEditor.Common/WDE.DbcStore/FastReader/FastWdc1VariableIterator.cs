using System;
using System.Text;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader;

public partial class FastWdc1Reader
{
    private class VariableIterator : IDbcIterator
    {
        private readonly FastWdc1Reader parent;
        private int index;
        private int offset;
        private int size;
        private int lastKnownFieldIndex;
        private int lastKnownFieldOffset;
        private int lastKnownFieldSize;

        public VariableIterator(FastWdc1Reader parent)
        {
            this.parent = parent;
        }

        public void SetOffsetIndex(int index, int offset, int size)
        {
            this.index = index;
            this.offset = offset;
            this.size = size;
            lastKnownFieldIndex = -1;
            lastKnownFieldOffset = 0;
            lastKnownFieldSize = 0;
        }

        private ReadOnlySpan<byte> GetRowMemory()
        {
            return parent.data.Span.Slice(offset, size);
        }

        // getting the field offset is a bit complex in this case
        // because it depends on the previous field
        private int GetFieldBitOffset(int field)
        {
            if (lastKnownFieldIndex == field) // reading the same field, well, if you want :shrug:
                return lastKnownFieldOffset;
            if (lastKnownFieldIndex + 1 == field) // we are reading the subsequent field
            {
                lastKnownFieldIndex = field;
                lastKnownFieldOffset = lastKnownFieldOffset + lastKnownFieldSize;
                lastKnownFieldSize = parent.fieldStorageInfo[field].FieldSizeBits;
            }
            else if (lastKnownFieldIndex > field)
                throw new Exception("Due to the way how this works, you can only read fields in order");
            else if (lastKnownFieldIndex < field)
            {
                // this may not be correct if inbetween there are string fields!
                while (lastKnownFieldIndex != field)
                {
                    lastKnownFieldIndex++;
                    lastKnownFieldOffset += lastKnownFieldSize;
                    lastKnownFieldSize = parent.fieldStorageInfo[lastKnownFieldIndex].FieldSizeBits;
                }
            }

            return lastKnownFieldOffset;
        }


        public uint Key
        {
            get
            {
                if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
                    return parent.ids.Span.Slice(index * 4, 4).ReadUInt();
                throw new Exception("This WDC1 doesn't have a key");
            }
        }

        // we assume that the ReadOnlySpan<byte> has at least 7 more bytes at the end
        private unsafe ulong ReadBits(ReadOnlySpan<byte> mem, int bitOffset, int bitCount)
        {
            var byteOffset = bitOffset / 8;
            fixed (byte* ptr = mem)
            {
                var asLongValue = *(ulong*)(ptr + byteOffset);
                var longValue = (asLongValue << (64 - bitCount - (bitOffset & 7))) >> (64 - bitCount);
                return longValue;
            }
        }

        public ulong GetULong(int field)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
            {
                if (field == 0)
                    return Key;
                field--;
            }

            if (parent.fieldStorageInfo[field].StorageType >= FieldCompression.FieldCompressionCommonData)
                throw new Exception("not implemented");
            return ReadBits(GetRowMemory(), GetFieldBitOffset(field), parent.fieldStorageInfo[field].FieldSizeBits);
        }

        public ulong GetULong(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public long GetLong(int field)
        {
            throw new NotImplementedException();
        }

        public long GetLong(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public sbyte GetSbyte(int field)
        {
            throw new NotImplementedException();
        }

        public sbyte GetSbyte(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int field)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int GetInt(int field) => (int)GetULong(field);
        public int GetInt(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public uint GetUInt(int field) => (uint)GetULong(field);

        public uint GetUInt(int field, int index)
        {
            throw new NotImplementedException();
        }

        public ushort GetUShort(int field, int index)
        {
            throw new NotImplementedException();
        }

        public ushort GetUShort(int field) => (ushort)GetULong(field);

        public string GetString(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public unsafe float GetFloat(int field)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
                field--;
            if (parent.fieldStorageInfo[field].StorageType >= FieldCompression.FieldCompressionCommonData)
                throw new Exception("not implemented");
            var bitOffset = GetFieldBitOffset(field);
            fixed (byte* ptr = GetRowMemory())
                return *(float*)(ptr + bitOffset / 8);
        }

        public float GetFloat(int field, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public string GetString(int field)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
                field--;

            var data = parent.data.Span;
            var start = offset + GetFieldBitOffset(field) / 8;
            var zeroByteIndex = start;
            while (data[zeroByteIndex++] != 0) ;
            lastKnownFieldSize = (zeroByteIndex - start) * 8; // including null byte
            return zeroByteIndex <= start
                ? ""
                : Encoding.UTF8.GetString(data.Slice(start, zeroByteIndex - start - 1));
        }
    }

}