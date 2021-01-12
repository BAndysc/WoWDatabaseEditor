using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDBXEditor.Storage;
using static WDBXEditor.Common.Constants;

namespace WDBXEditor.Reader.FileTypes
{
    public class WCH5 : DBHeader
    {
        protected int OffsetMapOffset = 0x30;

        protected WDB5 WDB5CounterPart;

        public WCH5()
        {
            HeaderSize = 0x30;
        }

        public WCH5(string filename)
        {
            HeaderSize = 0x30;
            FileName = filename;
        }

        public uint Build { get; set; }
        public uint TimeStamp { get; set; }
        public override bool ExtendedStringTable => true;

        public string FileName { get; set; }
        public override bool HasOffsetTable => Flags.HasFlag(HeaderFlags.OffsetMap);
        public override bool HasIndexTable => Flags.HasFlag(HeaderFlags.IndexMap);
        public override bool HasRelationshipData => Flags.HasFlag(HeaderFlags.RelationshipData);

        #region Read

        public override void ReadHeader(ref BinaryReader dbReader, string signature)
        {
            string _filename = Path.GetFileNameWithoutExtension(FileName).ToLower();
            WDB5CounterPart = Database.Entries.FirstOrDefault(x =>
                    x.Header.IsTypeOf<WDB5>() && Path.GetFileNameWithoutExtension(x.FileName).ToLower() == _filename)
                ?.Header as WDB5;

            if (WDB5CounterPart == null)
                throw new Exception("You must have the DB2 counterpart open first to be able to read this file.");

            Flags = WDB5CounterPart.Flags;
            IdIndex = WDB5CounterPart.IdIndex;
            FieldStructure = WDB5CounterPart.FieldStructure;

            if (HasOffsetTable)
                Flags = HeaderFlags.OffsetMap;

            base.ReadHeader(ref dbReader, signature);
            TableHash = dbReader.ReadUInt32();
            LayoutHash = dbReader.ReadInt32();
            Build = dbReader.ReadUInt32();
            TimeStamp = dbReader.ReadUInt32();
            MinId = dbReader.ReadInt32();
            MaxId = dbReader.ReadInt32();
            Locale = dbReader.ReadInt32();
        }

        public Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
        {
            var CopyTable = new Dictionary<int, byte[]>();
            var offsetmap = new List<OffsetEntry>();

            long indexTablePos = dbReader.BaseStream.Length - (HasIndexTable ? RecordCount * 4 : 0);
            int[] m_indexes = null;

            //Offset Map - Contains the index, offset and length so the index table is not used
            if (HasOffsetTable)
            {
                // Records table
                if (StringBlockSize > 0)
                    dbReader.Scrub(StringBlockSize);

                for (var i = 0; i < RecordCount; i++)
                {
                    int id = dbReader.ReadInt32();
                    int offset = dbReader.ReadInt32();
                    short length = dbReader.ReadInt16();

                    if (offset == 0 || length == 0)
                        continue;

                    offsetmap.Add(new OffsetEntry(id, offset, length));
                }
            }

            //Index table
            if (HasIndexTable)
            {
                if (!HasOffsetTable || HasRelationshipData)
                    dbReader.Scrub(indexTablePos);

                m_indexes = new int[RecordCount];
                for (var i = 0; i < RecordCount; i++)
                    m_indexes[i] = dbReader.ReadInt32();
            }

            //Extract record data
            for (var i = 0; i < Math.Max(RecordCount, offsetmap.Count); i++)
            {
                if (HasOffsetTable)
                {
                    OffsetEntry map = offsetmap[i];
                    dbReader.Scrub(map.Offset);

                    var recordbytes = BitConverter.GetBytes(map.Id).Concat(dbReader.ReadBytes(map.Length));
                    CopyTable.Add(map.Id, recordbytes.ToArray());
                }
                else
                {
                    dbReader.Scrub(pos + i * RecordSize);
                    byte[] recordbytes = dbReader.ReadBytes((int) RecordSize);

                    if (HasIndexTable)
                    {
                        var newrecordbytes = BitConverter.GetBytes(m_indexes[i]).Concat(recordbytes);
                        CopyTable.Add(m_indexes[i], newrecordbytes.ToArray());
                    }
                    else
                    {
                        int bytecount = FieldStructure[IdIndex].ByteCount;
                        int offset = FieldStructure[IdIndex].Offset;

                        var id = 0;
                        for (var j = 0; j < bytecount; j++)
                            id |= recordbytes[offset + j] << (j * 8);

                        CopyTable.Add(id, recordbytes);
                    }
                }
            }

            return CopyTable;
        }

        public override byte[] ReadData(BinaryReader dbReader, long pos)
        {
            var CopyTable = ReadOffsetData(dbReader, pos);
            OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
            return CopyTable.Values.SelectMany(x => x).ToArray();
        }

        internal struct OffsetEntry
        {
            public int Id { get; set; }
            public int Offset { get; set; }
            public short Length { get; set; }

            public OffsetEntry(int id, int offset, short length)
            {
                Id = id;
                Offset = offset;
                Length = length;
            }
        }

        #endregion
    }
}