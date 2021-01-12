using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WDBXEditor.Reader.FileTypes
{
    internal class WCH7 : WCH5
    {
        public WCH7()
        {
            StringTableOffset = 0x14;
            HeaderSize = 0x34;
        }

        public WCH7(string filename)
        {
            StringTableOffset = 0x14;
            HeaderSize = 0x34;
            FileName = filename;
        }

        public int[] WCH7Table { get; private set; } = new int[0];

        public override byte[] ReadData(BinaryReader dbReader, long pos)
        {
            var CopyTable = ReadOffsetData(dbReader, pos);
            OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
            return CopyTable.Values.SelectMany(x => x).ToArray();
        }

        public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
        {
            var CopyTable = new Dictionary<int, byte[]>();
            var offsetmap = new List<OffsetEntry>();

            long indexTablePos = dbReader.BaseStream.Length - (HasIndexTable ? RecordCount * 4 : 0);
            long wch7TablePos = indexTablePos - UnknownWCH7 * 4;
            int[] m_indexes = null;


            //Offset table - Contains the index, offset and length meaning the index table is not used
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

            //New WCH7 table
            if (UnknownWCH7 > 0)
            {
                WCH7Table = new int[UnknownWCH7];
                dbReader.Scrub(wch7TablePos);

                for (var i = 0; i < UnknownWCH7; i++)
                    WCH7Table[i] = dbReader.ReadInt32();
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
    }
}