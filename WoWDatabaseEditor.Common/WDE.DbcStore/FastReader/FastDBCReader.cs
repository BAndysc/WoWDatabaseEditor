using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader
{
    public class FastDbcReader : IEnumerable<IDbcIterator>
    {
        public static readonly uint WDBC = 0x43424457;
        private readonly byte[] bytes;
        private uint recordsSizeInBytes;
        private uint recordsOffsetInBytes;
        private uint stringsOffsetInBytes;
        private uint recordCount;
        private uint recordSize;

        public FastDbcReader(string path) : this(File.ReadAllBytes(path))
        {
        }

        public FastDbcReader(byte[] bytes)
        {
            this.bytes = bytes;
            uint magic = BitConverter.ToUInt32(this.bytes, 0);

            if (magic != WDBC)
                throw new Exception("File is not valid DBC!");
            
            recordCount = BitConverter.ToUInt32(this.bytes, 4);
            uint fieldCount = BitConverter.ToUInt32(this.bytes, 8);
            recordSize = BitConverter.ToUInt32(this.bytes, 12);
            uint stringTableSize = BitConverter.ToUInt32(this.bytes, 16);
            
            recordsSizeInBytes = recordCount * recordSize;
            recordsOffsetInBytes = 20;
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
            private readonly FastDbcReader parent;
            private int offset;

            public Iterator(FastDbcReader parent)
            {
                this.parent = parent;
            }

            public void SetOffset(int offset)
            {
                this.offset = offset;
            }
            
            public int GetInt(int field) => BitConverter.ToInt32(parent.bytes, offset + field * 4);

            public uint GetUInt(int field) => BitConverter.ToUInt32(parent.bytes, offset + field * 4);

            public float GetFloat(int field) => BitConverter.ToSingle(parent.bytes, offset + field * 4);

            public string GetString(int field)
            {
                var start = (int)(parent.stringsOffsetInBytes + GetUInt(field));
                var zeroByteIndex = start;
                while (parent.bytes[zeroByteIndex++] != 0) ;
                return zeroByteIndex <= start ? "" : Encoding.ASCII.GetString(parent.bytes, start, zeroByteIndex - start - 1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}