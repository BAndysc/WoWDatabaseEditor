using System;
using System.IO;

namespace WDE.DbcStore.FastReader;

internal class Wdc1Header
{
    public const int HeaderSizeOf = 84;

    public uint Magic; // 'WDC1'
    public uint RecordCount;
    public uint FieldCount;
    public uint RecordSize;
    public uint StringTableSize;
    public uint TableHash; // hash of the table name
    public uint LayoutHash; // this is a hash field that changes only when the structure of the data changes
    public uint MinId;
    public uint MaxId;
    public uint Locale; // as seen in TextWowEnum
    public uint CopyTableSize;
    public HeaderFlags Flags; // possible values are listed in Known Flag Meanings
    public ushort IdIndex; // this is the index of the field containing ID values; this is ignored if flags & 0x04 != 0
    public uint TotalFieldCount; // from WDC1 onwards, this value seems to always be the same as the 'field_count' value

    public uint BitpackedDataOffset; // relative position in record where bitpacked data begins; not important for parsing the file

    public uint LookupColumnCount;
    public uint OffsetMapOffset; // Offset to array of struct {uint32_t offset; uint16_t size;}[max_id - min_id + 1];
    public uint IdListSize; // List of ids present in the DB file
    public uint FieldStorageInfoSize;
    public uint CommonDataSize;
    public uint PalletDataSize;
    public uint RelationshipDataSize;
    public FieldStructure[] Fields;

    public struct FieldStructure
    {
        public const uint SizeOf = 4;

        public short SizeInBytes; // size in bits as calculated by: byteSize = (32 - size) / 8; this value can be negative to indicate field sizes larger than 32-bits

        public ushort Position; // position of the field within the record, relative to the start of the record
    }

    [Flags]
    public enum HeaderFlags : short
    {
        None = 0x0,
        OffsetMap = 0x1,
        RelationshipData = 0x2,
        IndexMap = 0x4,
        Unknown = 0x8,
        Compressed = 0x10
    }

    public Wdc1Header(BinaryReader binaryReader)
    {
        Magic = binaryReader.ReadUInt32();
        RecordCount = binaryReader.ReadUInt32();
        FieldCount = binaryReader.ReadUInt32();
        RecordSize = binaryReader.ReadUInt32();
        StringTableSize = binaryReader.ReadUInt32();
        TableHash = binaryReader.ReadUInt32();
        LayoutHash = binaryReader.ReadUInt32();
        MinId = binaryReader.ReadUInt32();
        MaxId = binaryReader.ReadUInt32();
        Locale = binaryReader.ReadUInt32();
        CopyTableSize = binaryReader.ReadUInt32();
        Flags = (HeaderFlags)binaryReader.ReadUInt16();
        IdIndex = binaryReader.ReadUInt16();
        TotalFieldCount = binaryReader.ReadUInt32();
        BitpackedDataOffset = binaryReader.ReadUInt32();
        LookupColumnCount = binaryReader.ReadUInt32();
        OffsetMapOffset = binaryReader.ReadUInt32();
        IdListSize = binaryReader.ReadUInt32();
        FieldStorageInfoSize = binaryReader.ReadUInt32();
        CommonDataSize = binaryReader.ReadUInt32();
        PalletDataSize = binaryReader.ReadUInt32();
        RelationshipDataSize = binaryReader.ReadUInt32();

        Fields = new FieldStructure[TotalFieldCount];
        for (int i = 0; i < TotalFieldCount; ++i)
        {
            Fields[i] = new FieldStructure()
            {
                SizeInBytes = (short)((32 - binaryReader.ReadInt16()) / 8),
                Position = binaryReader.ReadUInt16(),
            };
        }
    }
}