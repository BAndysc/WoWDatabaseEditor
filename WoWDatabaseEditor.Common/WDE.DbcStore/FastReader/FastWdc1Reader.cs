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
    private ArraySlice<byte> pallet;
    
    private RelationshipMapping? relationship;

    private FieldStorageInfo[] fieldStorageInfo;

    private struct OffsetMapEntry
    {
        public const int SizeOf = 6;
        public uint Offset;
        public ushort Size;
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
        public uint NumEntries;
        public uint MinId;
        public uint MaxId;
        public RelationshipEntry[] Entries;
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

        if (header.CopyTableSize > 0)
        {
            // skip
            reader.BaseStream.Position += header.CopyTableSize;
        }

        fieldStorageInfo = new FieldStorageInfo[header.FieldStorageInfoSize / FieldStorageInfo.SizeOf];
        for (var i = 0; i < fieldStorageInfo.Length; i++)
        {
            fieldStorageInfo[i] = new FieldStorageInfo(reader);
        }

        pallet = new ArraySlice<byte>(bytes, (int)reader.BaseStream.Position, (int)header.PalletDataSize);
        
        reader.BaseStream.Position += header.PalletDataSize;
        reader.BaseStream.Position += header.CommonDataSize;

        if (header.RelationshipDataSize > 0)
        {
            relationship = new RelationshipMapping();
            relationship.NumEntries = reader.ReadUInt32();
            relationship.MinId = reader.ReadUInt32();
            relationship.MaxId = reader.ReadUInt32();
            relationship.Entries = new RelationshipEntry[relationship.NumEntries];
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
    }

    public FastWdc1Reader(string path) : this(File.ReadAllBytes(path))
    {
    }

    public IEnumerator<IDbcIterator> GetEnumerator()
    {
        if (header.Flags.HasFlagFast(Wdc1Header.HeaderFlags.OffsetMap))
        {
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
        else
        {
            RegularIterator iterator = new RegularIterator(this);
            for (int i = 0; i < RecordCount; ++i)
            {
                iterator.SetOffsetIndex(i, i * header.RecordSize);
                yield return iterator;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public uint RecordCount => header.RecordCount;
}