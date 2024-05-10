using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader
{
    public class FastDb2Reader : IDBC
    {
        public static readonly uint WDB2 = 0x32424457;
        private readonly byte[] bytes;
        private uint recordsSizeInBytes;
        private uint recordsOffsetInBytes;
        private uint stringsOffsetInBytes;
        private uint recordCount;
        private uint recordSize;

        public FastDb2Reader(string path) : this(File.ReadAllBytes(path))
        {
        }

        public FastDb2Reader(byte[] bytes)
        {
            this.bytes = bytes;
            uint magic = BitConverter.ToUInt32(this.bytes, 0);

            if (magic != WDB2)
                throw new Exception("File is not db2!");
            
            recordCount = BitConverter.ToUInt32(this.bytes, 4);
            uint fieldCount = BitConverter.ToUInt32(this.bytes, 8);
            recordSize = BitConverter.ToUInt32(this.bytes, 12);
            uint stringTableSize = BitConverter.ToUInt32(this.bytes, 16);
            uint tableHash = BitConverter.ToUInt32(this.bytes, 20);
            uint build = BitConverter.ToUInt32(this.bytes, 24);
            uint timestampLastWritten = BitConverter.ToUInt32(this.bytes, 28);
            uint minId = BitConverter.ToUInt32(this.bytes, 32);
            uint maxId = BitConverter.ToUInt32(this.bytes, 36);
            uint locale = BitConverter.ToUInt32(this.bytes, 40);
            uint copyTableSize = BitConverter.ToUInt32(this.bytes, 44);

            uint mappingSizeInBytes = 0;
            if (maxId != 0)
                mappingSizeInBytes = (maxId - minId + 1) * 6;

            recordsSizeInBytes = recordCount * recordSize;
            recordsOffsetInBytes = 48 + mappingSizeInBytes;
            stringsOffsetInBytes = recordsOffsetInBytes + recordsSizeInBytes;
        }

        public IEnumerator<IDbcIterator> GetEnumerator()
        {
            Iterator iterator = new Iterator(this);
            for (uint i = 0; i < recordCount; ++i)
            {
                iterator.SetOffset((int)(recordsOffsetInBytes + i * recordSize));
                yield return iterator;
            }
        }

        private class Iterator : IDbcIterator
        {
            private readonly FastDb2Reader parent;
            private int offset;

            public Iterator(FastDb2Reader parent)
            {
                this.parent = parent;
            }

            public void SetOffset(int offset)
            {
                this.offset = offset;
            }

            public uint Key => throw new Exception("DB2 does not have a key");

            public int GetInt(int field) => BitConverter.ToInt32(parent.bytes, offset + field * 4);

            public int GetInt(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public uint GetUInt(int field) => BitConverter.ToUInt32(parent.bytes, offset + field * 4);

            public string GetString(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public float GetFloat(int field) => BitConverter.ToSingle(parent.bytes, offset + field * 4);
            
            public float GetFloat(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public uint GetUInt(int field, int index) => throw new Exception("DB2 doesn't have arrays");

            public ushort GetUShort(int field) => (ushort)GetUInt(field);
            
            public ushort GetUShort(int field, int index) => throw new Exception("DB2 doesn't have arrays");

            public ulong GetULong(int field) => GetUInt(field);

            public ulong GetULong(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public long GetLong(int field) => GetInt(field);

            public long GetLong(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public sbyte GetSbyte(int field) => (sbyte)GetInt(field);

            public sbyte GetSbyte(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public byte GetByte(int field) => (byte)GetUInt(field);

            public byte GetByte(int field, int arrayIndex) => throw new Exception("DB2 doesn't have arrays");

            public string GetString(int field)
            {
                var start = (int)(parent.stringsOffsetInBytes + GetUInt(field));
                var zeroByteIndex = start;
                while (parent.bytes[zeroByteIndex++] != 0) ;
                return zeroByteIndex <= start ? "" : Encoding.UTF8.GetString(parent.bytes, start, zeroByteIndex - start - 1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public uint RecordCount => recordCount;
    }
}