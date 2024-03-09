using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader;

public partial class FastWdc1Reader : IDBC
{
    public static readonly uint WDC1 = 0x31434457;
    private Wdc1Header header;

    private ArraySlice<byte> data;
    private ArraySlice<byte> strings;
    private ArraySlice<byte> offsets;
    private ArraySlice<byte> ids;
    private ArraySlice<byte> copy;
    private ArraySlice<byte>[]? palletOrCommon;
    
    private RelationshipMapping? relationship;

    private FieldStorageInfo[] fieldStorageInfo;

    private struct OffsetMapEntry
    {
        public const int SizeOf = 6;
        public uint Offset = 0;
        public ushort Size = 0;

        public OffsetMapEntry() { }
    }
    
    private enum FieldCompression
    {
        // None -- the field is a 8-, 16-, 32-, or 64-bit integer in the record data
        FieldCompressionNone,

        // Bitpacked -- the field is a bitpacked integer in the record data.  It
        // is field_size_bits long and starts at field_offset_bits.
        // A bitpacked value occupies
        //   (field_size_bits + (field_offset_bits & 7) + 7) / 8
        // bytes starting at byte
        //   field_offset_bits / 8
        // in the record data.  These bytes should be read as a little-endian value,
        // then the value is shifted to the right by (field_offset_bits & 7) and
        // masked with ((1ull << field_size_bits) - 1).
        FieldCompressionBitpacked,

        // Common data -- the field is assumed to be a default value, and exceptions
        // from that default value are stored in the corresponding section in
        // common_data as pairs of { uint32_t record_id; uint32_t value; }.
        FieldCompressionCommonData,

        // Bitpacked indexed -- the field has a bitpacked index in the record data.
        // This index is used as an index into the corresponding section in
        // pallet_data.  The pallet_data section is an array of uint32_t, so the index
        // should be multiplied by 4 to obtain a byte offset.
        FieldCompressionBitpackedIndexed,

        // Bitpacked indexed array -- the field has a bitpacked index in the record
        // data.  This index is used as an index into the corresponding section in
        // pallet_data.  The pallet_data section is an array of uint32_t[array_count],
        //
        FieldCompressionBitpackedIndexedArray,
    }

    private struct FieldStorageInfo
    {
        public const ulong SizeOf = 24;
        public ushort FieldOffsetBits;
        public ushort FieldSizeBits; // very important for reading bitpacked fields; size is the sum of all array pieces in bits - for example, uint32[3] will appear here as '96'

        // additional_data_size is the size in bytes of the corresponding section in
        // common_data or pallet_data.  These sections are in the same order as the
        // field_info, so to find the offset, add up the additional_data_size of any
        // previous fields which are stored in the same block (common_data or
        // pallet_data).
        public uint AdditionalDataSize;
        public FieldCompression StorageType;

        public uint BitpackingOffsetBits; // not useful for most purposes; formula they use to calculate is bitpacking_offset_bits = field_offset_bits - (header.bitpacked_data_offset * 8)

        public uint BitpackingSizeBits; // not useful for most purposes
        public uint Flags; // known values - 0x01: sign-extend (signed)
        public uint DefaultValue => BitpackingOffsetBits;
        public uint ArrayCount => Flags; //  for FieldCompressionBitpackedIndexedArray

        public FieldStorageInfo(BinaryReader reader)
        {
            FieldOffsetBits = reader.ReadUInt16();
            FieldSizeBits = reader.ReadUInt16();
            AdditionalDataSize = reader.ReadUInt32();
            StorageType = (FieldCompression)reader.ReadUInt32();
            BitpackingOffsetBits = reader.ReadUInt32();
            BitpackingSizeBits = reader.ReadUInt32();
            Flags = reader.ReadUInt32();
        }
    }

    private struct RelationshipEntry
    {
        // This is the id of the foreign key for the record, e.g. SpellID in
        // SpellX* tables.
        public uint ForeignId;

        // This is the index of the record in record_data.  Note that this is
        // *not* the record's own ID.
        public uint RecordIndex;
    }

    private class RelationshipMapping
    {
        public readonly uint NumEntries;
        public readonly uint MinId;
        public readonly uint MaxId;
        public readonly RelationshipEntry[] Entries;

        public RelationshipMapping(uint numEntries, uint minId, uint maxId, RelationshipEntry[] entries)
        {
            NumEntries = numEntries;
            MinId = minId;
            MaxId = maxId;
            Entries = entries;
        }
    }

    public FastWdc1Reader(byte[] bytes)
    {
        var reader = new BinaryReader(new MemoryStream(bytes), System.Text.Encoding.UTF8);
        header = new Wdc1Header(reader);

        if (header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.OffsetMap))
        {
            int start = (int)reader.BaseStream.Position;
            var length = (int)(header.OffsetMapOffset - Wdc1Header.HeaderSizeOf - (Wdc1Header.FieldStructure.SizeOf) * header.TotalFieldCount);
            data = new ArraySlice<byte>(bytes, start, length);
            var mapLength = OffsetMapEntry.SizeOf * (int)(header.MaxId - header.MinId + 1);
            offsets = new ArraySlice<byte>(bytes, start + length, mapLength);
            reader.BaseStream.Position = start + length + mapLength;
        }
        else
        {
            int start = (int)reader.BaseStream.Position;
            var length = (int)(header.RecordCount * header.RecordSize);
            data = new ArraySlice<byte>(bytes, start, length);
            strings = new ArraySlice<byte>(bytes, start + length, (int)(header.StringTableSize));
            reader.BaseStream.Position = start + length + header.StringTableSize;
        }

        ids = new ArraySlice<byte>(bytes, (int)reader.BaseStream.Position, (int)header.IdListSize);
        reader.BaseStream.Position += header.IdListSize;

        copy = new ArraySlice<byte>(bytes, (int)reader.BaseStream.Position, (int)header.CopyTableSize);
        reader.BaseStream.Position += header.CopyTableSize;
        
        fieldStorageInfo = new FieldStorageInfo[header.FieldStorageInfoSize / FieldStorageInfo.SizeOf];
        for (var i = 0; i < fieldStorageInfo.Length; i++)
        {
            fieldStorageInfo[i] = new FieldStorageInfo(reader);
        }

        var palletStart = reader.BaseStream.Position;
        for (int i = 0; i < fieldStorageInfo.Length; ++i)
        {
            if (fieldStorageInfo[i].AdditionalDataSize == 0)
                continue;
            
            if (fieldStorageInfo[i].StorageType is FieldCompression.FieldCompressionBitpackedIndexedArray or FieldCompression.FieldCompressionBitpackedIndexed)
            {
                palletOrCommon ??= new ArraySlice<byte>[fieldStorageInfo.Length];
                palletOrCommon[i] = new ArraySlice<byte>(bytes, (int)reader.BaseStream.Position, (int)fieldStorageInfo[i].AdditionalDataSize);
                reader.BaseStream.Position += fieldStorageInfo[i].AdditionalDataSize;   
            }
        }
        
        if (palletStart + header.PalletDataSize != reader.BaseStream.Position)
            throw new Exception("pallet data size mismatch");

        var commonStart = reader.BaseStream.Position;
        for (int i = 0; i < fieldStorageInfo.Length; ++i)
        {
            if (fieldStorageInfo[i].AdditionalDataSize == 0)
                continue;
            
            if (fieldStorageInfo[i].StorageType is FieldCompression.FieldCompressionCommonData)
            {
                palletOrCommon ??= new ArraySlice<byte>[fieldStorageInfo.Length];
                palletOrCommon[i] = new ArraySlice<byte>(bytes, (int)reader.BaseStream.Position, (int)fieldStorageInfo[i].AdditionalDataSize);
                reader.BaseStream.Position += fieldStorageInfo[i].AdditionalDataSize;   
            }
        }
        if (commonStart + header.CommonDataSize != reader.BaseStream.Position)
            throw new Exception("common data size mismatch");

        if (header.RelationshipDataSize > 0)
        {
            var numEntries = reader.ReadUInt32();
            var minId = reader.ReadUInt32();
            var maxId = reader.ReadUInt32();
            relationship = new RelationshipMapping(numEntries, minId, maxId, new RelationshipEntry[numEntries]);
            for (int i = 0; i < relationship.NumEntries; ++i)
            {
                relationship.Entries[i] = new RelationshipEntry()
                {
                    ForeignId = reader.ReadUInt32(),
                    RecordIndex = reader.ReadUInt32(),
                };
                if (relationship.Entries[i].RecordIndex != i)
                    throw new Exception("non linear relationship indices");
            }
        }

        RecordCount = header.RecordCount + (uint)copy.Length / 8;
    }

    public FastWdc1Reader(string path) : this(File.ReadAllBytes(path))
    {
    }

    public IEnumerator<IDbcIterator> GetEnumerator()
    {
        if (header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.OffsetMap))
        {
            if (copy.Length > 0)
                throw new Exception("Copy not implemented with offset");
            
            VariableIterator iterator = new VariableIterator(this);
            for (int i = 0; i < header.RecordCount; ++i)
            {
                var id = ids.Span.Slice(i * 4, 4).ReadInt();
                var offset = offsets.Span.Slice((id - (int)header.MinId) * 6, 4).ReadInt() - data.Start;
                var size = offsets.Span.Slice((id - (int)header.MinId) * 6 + 4, 2).ReadShort();
                iterator.SetOffsetIndex(i, offset, size);
                yield return iterator;
            }
        }
        else if (header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.IndexMap) && header.CopyTableSize > 0)
        {
            // I have to construct a list, because it is not sorted.
            List<(uint recordId, uint originalRecordId)> copyMapping = new();
            while (copy.Length > 0)
            {
                var view = new CopyDataElementView(copy.Span);
                copyMapping.Add((view.RecordId, view.OldRecordId));
                ForwardCopyData();
            }
            copyMapping.Sort();

            RegularIterator iterator = new RegularIterator(this);
            Dictionary<uint, int> recordIdToIndex = new();

            int copyMappingIndex = 0;
            for (int i = 0; i < header.RecordCount; ++i)
            {
                iterator.SetOffsetIndex(i, i * header.RecordSize);
                var recordId = iterator.Key;
                recordIdToIndex[recordId] = i;
                
                while (copyMappingIndex < copyMapping.Count && copyMapping[copyMappingIndex].recordId < recordId)
                {
                    var oldIndex = recordIdToIndex[copyMapping[copyMappingIndex].originalRecordId];
                    RegularIterator copyShadow = new RegularIterator(this);
                    copyShadow.SetOffsetIndex(oldIndex, oldIndex * header.RecordSize);
                    copyShadow.OverrideKey(copyMapping[copyMappingIndex].recordId);
                    yield return copyShadow;

                    copyMappingIndex++;
                }
                yield return iterator;
            }

            while (copyMappingIndex < copyMapping.Count)
            {
                var oldIndex = recordIdToIndex[copyMapping[copyMappingIndex].originalRecordId];
                RegularIterator copyShadow = new RegularIterator(this);
                copyShadow.SetOffsetIndex(oldIndex, oldIndex * header.RecordSize);
                copyShadow.OverrideKey(copyMapping[copyMappingIndex].recordId);
                yield return copyShadow;
                copyMappingIndex++;
            }
        }
        else
        {
            
            if (copy.Length > 0)
                throw new Exception("Copy not implemented without indexmap");

            RegularIterator iterator = new RegularIterator(this);
            
            for (int i = 0; i < RecordCount; ++i)
            {
                iterator.SetOffsetIndex(i, i * header.RecordSize);
                yield return iterator;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public uint RecordCount { get; }

    private void ForwardCommonData(int field)
    {
        palletOrCommon![field] = palletOrCommon[field].Skip(8);
    }

    private void ForwardCopyData()
    {
        copy = copy.Skip(8);
    }
}