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
        private uint? overrideKey;

        public RegularIterator(FastWdc1Reader parent)
        {
            this.parent = parent;
        }

        public void SetOffsetIndex(int index, long offset)
        {
            this.index = index;
            this.offset = (int)offset;
            overrideKey = null;
        }

        private ReadOnlySpan<byte> GetRowMemory()
        {
            return parent.data.Span.Slice(offset, (int)parent.header.RecordSize);
        }

        private ReadOnlySpan<byte> GetFieldMemory(int fieldId, int arrayIndex = 0)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
            {
                if (fieldId == 0)
                    return parent.ids.Span.Slice(index * 4, 4);

                fieldId--;
            }
            var offsetBits = parent.fieldStorageInfo[fieldId].FieldOffsetBits;
            var sizeTotalBits = parent.fieldStorageInfo[fieldId].FieldSizeBits;
            var elementSize = (parent.header.Fields[fieldId].SizeInBytes * 8);
            var elementsCount = sizeTotalBits / elementSize;
            
            return parent.data.Span.Slice(offset + offsetBits / 8 + arrayIndex * (elementSize / 8), elementSize / 8);
        }

        public uint Key
        {
            get
            {
                if (overrideKey.HasValue)
                    return overrideKey.Value;
                if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
                    return parent.ids.Span.Slice(index * 4, 4).ReadUInt();
                return GetUInt(parent.header.IdIndex);
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

        public unsafe ulong GetULong(int field)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
            {
                if (field == 0)
                    return Key;
                field--;
            }

            if (field == parent.fieldStorageInfo.Length && parent.relationship != null)
                return parent.relationship.Entries[index].ForeignId;

            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionCommonData)
            {
                var id = GetInt(parent.header.IdIndex);
                CommonDataElementView commonView = new(parent.palletOrCommon![field].Span);
                while (commonView.IsValid && commonView.RecordId < id)
                {
                    parent.ForwardCommonData(field);
                    commonView = new(parent.palletOrCommon[field].Span);
                }
                
                if (commonView.IsValid && commonView.RecordId == id)
                    return commonView.Value;
                return parent.fieldStorageInfo[field].DefaultValue;
            }
            
            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionBitpackedIndexedArray)
                throw new Exception("Cannot read bitpacked indexed array as ulong");
            
            var value = ReadBits(GetRowMemory(), parent.fieldStorageInfo[field].FieldOffsetBits,
                parent.fieldStorageInfo[field].FieldSizeBits);

            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionBitpackedIndexed)
            {
                fixed (byte* ptr = parent.palletOrCommon![field].Span.Slice((int)value * 4, 4))
                    return *(uint*)ptr;
            }

            return value;
        }

        public long GetLong(int field) => (long)GetULong(field);

        public long GetLong(int field, int arrayIndex) => (long)GetULong(field, arrayIndex);

        public sbyte GetSbyte(int field) => (sbyte)GetInt(field);

        public sbyte GetSbyte(int field, int arrayIndex) => (sbyte)GetInt(field, arrayIndex);

        public byte GetByte(int field) => (byte)GetUInt(field);

        public byte GetByte(int field, int arrayIndex) => (byte)GetUInt(field, arrayIndex);

        public unsafe ulong GetULong(int field, int index)
        {
            if (parent.header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap))
                field--;
            if (field == parent.fieldStorageInfo.Length && parent.relationship != null)
                return parent.relationship.Entries[index].ForeignId;
            if (parent.fieldStorageInfo[field].StorageType is FieldCompression.FieldCompressionCommonData)
                throw new Exception("not implemented");
            var offsetBits = parent.fieldStorageInfo[field].FieldOffsetBits;
            var sizeTotalBits = parent.fieldStorageInfo[field].FieldSizeBits;
            var elementSize = (parent.header.Fields[field].SizeInBytes * 8);
            var originalIndex = index;
            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionBitpackedIndexedArray)
            {
                index = 0;
                elementSize = sizeTotalBits;
            }
            var value = ReadBits(GetRowMemory(), offsetBits + index * elementSize, elementSize);
            
            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionBitpackedIndexed)
            {
                fixed (byte* ptr = parent.palletOrCommon![field].Span.Slice((int)value * 4, 4))
                    return *(uint*)ptr;
            }

            if (parent.fieldStorageInfo[field].StorageType == FieldCompression.FieldCompressionBitpackedIndexedArray)
            {
                fixed (byte* ptr = parent.palletOrCommon![field].Span.Slice((int)value * 4 * (int)parent.fieldStorageInfo[field].ArrayCount + originalIndex * 4, 4))
                    return *(uint*)ptr;
            }

            return value;
        }

        public int GetInt(int field) => (int)GetLong(field);
        
        public int GetInt(int field, int arrayIndex) => (int)GetLong(field, arrayIndex);

        public uint GetUInt(int field) => (uint)GetULong(field);

        public uint GetUInt(int field, int arrayIndex) => (uint)GetULong(field, arrayIndex);

        public ushort GetUShort(int field, int arrayIndex) => (ushort)GetULong(field, arrayIndex);

        public ushort GetUShort(int field) => (ushort)GetULong(field);

        public string GetString(int field, int arrayIndex)
        {
            var strings = parent.strings.Span;
            var start = GetInt(field, arrayIndex);
            var zeroByteIndex = start;
            while (strings[zeroByteIndex++] != 0) ;
            return zeroByteIndex <= start
                ? ""
                : Encoding.UTF8.GetString(strings.Slice(start, zeroByteIndex - start - 1));
        }

        public unsafe float GetFloat(int field)
        {
            fixed (byte* ptr = GetFieldMemory(field))
                return *(float*)ptr;
        }

        public unsafe float GetFloat(int field, int arrayIndex)
        {
            fixed (byte* ptr = GetFieldMemory(field, arrayIndex))
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

        public void OverrideKey(uint overrideKey)
        {
            this.overrideKey = overrideKey;
        }
    }
}