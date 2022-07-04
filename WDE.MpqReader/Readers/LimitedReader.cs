using System;

namespace WDE.MpqReader.Readers
{
    public class LimitedReader : IBinaryReader
    {
        private readonly IBinaryReader reader;
        private readonly int size;
        private readonly int startOffset;

        public LimitedReader(IBinaryReader reader, int size)
        {
            this.reader = reader;
            this.size = size;
            startOffset = reader.Offset;
        }
    
        private int SizeLeft => size - Offset;

        private void AssertCanRead(int length)
        {
            #if DEBUG
            if (length > SizeLeft)
                throw new IndexOutOfRangeException($"Cannot read {length} bytes, only {SizeLeft} left to read");
            #endif
        }
    
        public ReadOnlyMemory<byte> ReadBytes(int length)
        {
            AssertCanRead(length);
            return reader.ReadBytes(length);
        }

        public short ReadInt16()
        {
            AssertCanRead(2);
            return reader.ReadInt16();
        }

        public int ReadInt32()
        {
            AssertCanRead(4);
            return reader.ReadInt32();
        }

        public ushort ReadUInt16()
        {
            AssertCanRead(2);
            return reader.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            AssertCanRead(4);
            return reader.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            AssertCanRead(8);
            return reader.ReadUInt64();
        }

        public float ReadFloat()
        {
            AssertCanRead(4);
            return reader.ReadFloat();
        }

        public bool IsFinished()
        {
            return reader.IsFinished() || SizeLeft <= 0;
        }

        public byte ReadByte()
        {
            AssertCanRead(1);
            return reader.ReadByte();
        }

        public int Offset
        {
            get => reader.Offset - startOffset;
            set
            {
                if (value < 0)
                    throw new IndexOutOfRangeException("Cannot set offset smaller than desired range");
                if (value > size)
                    throw new IndexOutOfRangeException("Cannot set offset bigger than desired range");
                reader.Offset = value + startOffset;
            }
        }

        public int Size => size;
    }
}