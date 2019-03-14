using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDBXEditor.Storage;
using static WDBXEditor.Common.Constants;
using WDBXEditor.Common;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;

namespace WDBXEditor.Reader.FileTypes
{
    public class WDB5 : DBHeader
    {
        public override bool ExtendedStringTable => true;

        public override bool HasOffsetTable => Flags.HasFlag(HeaderFlags.OffsetMap);
        public override bool HasIndexTable => Flags.HasFlag(HeaderFlags.IndexMap);
        public override bool HasRelationshipData => Flags.HasFlag(HeaderFlags.RelationshipData);

        #region Read
        public void ReadHeader(BinaryReader dbReader, string signature)
        {
            ReadHeader(ref dbReader, signature);
        }

        public void ReadBaseHeader(ref BinaryReader dbReader, string signature)
        {
            base.ReadHeader(ref dbReader, signature);
        }

        public override void ReadHeader(ref BinaryReader dbReader, string signature)
        {
            base.ReadHeader(ref dbReader, signature);

            TableHash = dbReader.ReadUInt32();
            LayoutHash = dbReader.ReadInt32();
            MinId = dbReader.ReadInt32();
            MaxId = dbReader.ReadInt32();
            Locale = dbReader.ReadInt32();
            CopyTableSize = dbReader.ReadInt32();
            Flags = (HeaderFlags)dbReader.ReadUInt16();
            IdIndex = dbReader.ReadUInt16();

            if (Flags.HasFlag(HeaderFlags.IndexMap))
                IdIndex = 0; //Ignored if Index Table

            //Gather field structures
            FieldStructure = new List<FieldStructureEntry>();
            for (int i = 0; i < FieldCount; i++)
            {
                var field = new FieldStructureEntry(dbReader.ReadInt16(), (ushort)(dbReader.ReadUInt16() + (HasIndexTable ? 4 : 0)));
                FieldStructure.Add(field);

                if (i > 0)
                    FieldStructure[i - 1].SetLength(field);
            }

            if (HasIndexTable)
            {
				FieldCount++;
                FieldStructure.Insert(0, new FieldStructureEntry(0, 0));

                if (FieldCount > 1)
                    FieldStructure[1].SetLength(FieldStructure[0]);
            }
        }

        public Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
        {
            Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
            List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
            Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();

            long copyTablePos = dbReader.BaseStream.Length - CopyTableSize;
            long indexTablePos = copyTablePos - (HasIndexTable ? RecordCount * 4 : 0);
            int[] m_indexes = null;

            //Offset Map
            if (HasOffsetTable)
            {
                // Records table
                dbReader.Scrub(StringBlockSize);

                for (int i = 0; i < (MaxId - MinId + 1); i++)
                {
                    int offset = dbReader.ReadInt32();
                    short length = dbReader.ReadInt16();

                    if (offset == 0 || length == 0) continue;

                    //Special case, may contain duplicates in the offset map that we don't want
                    if (CopyTableSize == 0)
                    {
                        if (!firstindex.ContainsKey(offset))
                            firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
                        else
                            OffsetDuplicates.Add(MinId + i, firstindex[offset].VisibleIndex);
                    }

                    offsetmap.Add(new Tuple<int, short>(offset, length));
                }
            }

			if(HasRelationshipData)
				dbReader.BaseStream.Position += (MaxId - MinId + 1) * 4;

            //Index table
            if (HasIndexTable)
            {
                //Offset map alone reads straight into this others may not
                if (!HasOffsetTable || HasRelationshipData)
                    dbReader.Scrub(indexTablePos);

                m_indexes = new int[RecordCount];
                for (int i = 0; i < RecordCount; i++)
                    m_indexes[i] = dbReader.ReadInt32();
            }

            //Extract record data
            for (int i = 0; i < Math.Max(RecordCount, offsetmap.Count); i++)
            {
                if (HasOffsetTable)
                {
                    int id = m_indexes[CopyTable.Count];
                    var map = offsetmap[i];

                    if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
                        continue;

                    dbReader.Scrub(map.Item1);

                    IEnumerable<byte> recordbytes = BitConverter.GetBytes(id).Concat(dbReader.ReadBytes(map.Item2));
                    CopyTable.Add(id, recordbytes.ToArray());
                }
                else
                {
                    dbReader.Scrub(pos + i * RecordSize);
                    byte[] recordbytes = dbReader.ReadBytes((int)RecordSize);

                    if (HasIndexTable)
                    {
                        IEnumerable<byte> newrecordbytes = BitConverter.GetBytes(m_indexes[i]).Concat(recordbytes);
                        CopyTable.Add(m_indexes[i], newrecordbytes.ToArray());
                    }
                    else
                    {
                        int bytecount = FieldStructure[IdIndex].ByteCount;
                        int offset = FieldStructure[IdIndex].Offset;

                        int id = 0;
                        for (int j = 0; j < bytecount; j++)
                            id |= (recordbytes[offset + j] << (j * 8));

                        CopyTable.Add(id, recordbytes);
                    }
                }
            }

            //CopyTable
            if (CopyTableSize != 0 && copyTablePos != dbReader.BaseStream.Length)
            {
                dbReader.Scrub(copyTablePos);
                while (dbReader.BaseStream.Position != dbReader.BaseStream.Length)
                {
                    int id = dbReader.ReadInt32();
                    int idcopy = dbReader.ReadInt32();

                    byte[] copyRow = CopyTable[idcopy];
                    byte[] newRow = new byte[copyRow.Length];
                    Array.Copy(copyRow, newRow, newRow.Length);
                    Array.Copy(BitConverter.GetBytes(id), newRow, sizeof(int));

                    CopyTable.Add(id, newRow);
                }
            }

            return CopyTable;
        }

        public override byte[] ReadData(BinaryReader dbReader, long pos)
        {
            Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
            OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
            return CopyTable.Values.SelectMany(x => x).ToArray();
        }

        internal struct OffsetDuplicate
        {
            public int HiddenIndex { get; set; }
            public int VisibleIndex { get; set; }

            public OffsetDuplicate(int hidden, int visible)
            {
                this.HiddenIndex = hidden;
                this.VisibleIndex = visible;
            }
        }
        #endregion
        
    }
}
