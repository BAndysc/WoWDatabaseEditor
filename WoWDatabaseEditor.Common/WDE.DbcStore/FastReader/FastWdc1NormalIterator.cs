using System;
using System.Text;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader;

public partial class FastWdc1Reader
{
    private class RegularIterator : IDbcIterator
    {
        private readonly FastWdc1Reader parent;
        private int offset;
        private int index;

        public RegularIterator(FastWdc1Reader parent)
        {
            this.parent = parent;
        }

        public void SetOffsetIndex(int index, long offset)
        {
            this.index = index;
            this.offset = (int)offset;
        }

        private ReadOnlySpan<byte> GetRowMemory()
        {
            return parent.data.Span.Slice(offset, (int)parent.header.RecordSize);
        }

        private ReadOnlySpan<byte> GetFieldMemory(int fieldId)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
            {
                if (fieldId == 0)
                    return parent.ids.Span.Slice(index * 4, 4);

                fieldId--;
            }

            return parent.data.Span.Slice(offset + parent.header.Fields[fieldId].Position, parent.header.Fields[fieldId].SizeInBytes);
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

        private unsafe ulong GetULong(int field)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
            {
                if (field == 0)
                    return Key;
                field--;
            }

            if (field == parent.fieldStorageInfo.Length && parent.relationship != null)
                return parent.relationship.Entries[index].ForeignId;
            if (parent.fieldStorageInfo[field].StorageType is FieldCompression.FieldCompressionCommonData
                or FieldCompression.FieldCompressionBitpackedIndexedArray)
                throw new Exception("not implemented");
            var value = ReadBits(GetRowMemory(), parent.fieldStorageInfo[field].FieldOffsetBits,
                parent.fieldStorageInfo[field].FieldSizeBits);

            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionBitpackedIndexed)
            {
                fixed (byte* ptr = parent.pallet.Span.Slice((int)value * 4, 4))
                    return *(uint*)ptr;
            }

            return value;
        }


        private ulong GetULong(int field, int index)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
                field--;
            if (field == parent.fieldStorageInfo.Length && parent.relationship != null)
                return parent.relationship.Entries[index].ForeignId;
            if (parent.fieldStorageInfo[field].StorageType >= FieldCompression.FieldCompressionCommonData)
                throw new Exception("not implemented");
            var offsetBits = parent.fieldStorageInfo[field].FieldOffsetBits;
            var sizeTotalBits = parent.fieldStorageInfo[field].FieldSizeBits;
            var elementSize = (parent.header.Fields[field].SizeInBytes * 8);
            var elementsCount = sizeTotalBits / elementSize;
            return ReadBits(GetRowMemory(), offsetBits + index * elementSize, elementSize);
        }

        public int GetInt(int field) => (int)GetULong(field);

        public uint GetUInt(int field) => (uint)GetULong(field);

        public uint GetUInt(int field, int arrayIndex) => (uint)GetULong(field, arrayIndex);

        public ushort GetUShort(int field, int arrayIndex) => (ushort)GetULong(field, arrayIndex);

        public ushort GetUShort(int field) => (ushort)GetULong(field);

        public unsafe float GetFloat(int field)
        {
            fixed (byte* ptr = GetFieldMemory(field))
                return *(float*)ptr;
        }

        public string GetString(int field)
        {
            var strings = parent.strings.Span;
            var start = GetInt(field);
            var zeroByteIndex = start;
            while (strings[zeroByteIndex++] != 0) ;
            return zeroByteIndex <= start
                ? ""
                : Encoding.UTF8.GetString(strings.Slice(start, zeroByteIndex - start - 1));
        }
    }
}