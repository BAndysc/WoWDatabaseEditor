using System;
using TheMaths;

namespace WDE.MpqReader.Readers
{
    public class MemoryBinaryReader : IBinaryReader
    {
        private readonly byte[] bytes;
        private int currentPos;
        private readonly int end;
        public int Position => currentPos;

        public bool IsFinished()
        {
            return currentPos >= end;
        }

        public byte[] ReadBytes(int length)
        {
            byte[] result = new byte[length];
            Array.Copy(bytes, currentPos, result, 0, length);// Math.Min(length, bytes.Length - currentPos));
            currentPos += length;
            return result;
        }

        public short ReadInt16()
        {
            int val = bytes[currentPos] | (bytes[currentPos + 1] << 8);
            currentPos += 2;
            return (short)val;
        }

        public int ReadInt32()
        {
            int res = bytes[currentPos] | (bytes[currentPos + 1] << 8) | (bytes[currentPos + 2] << 16) | (bytes[currentPos + 3] << 24);
            currentPos += 4;
            return res;
        }

        public uint ReadUInt32()
        {
            uint res = (uint)(bytes[currentPos] | (bytes[currentPos + 1] << 8) | (bytes[currentPos + 2] << 16) | (bytes[currentPos + 3] << 24));
            currentPos += 4;
            return res;
        }

        public ulong ReadUInt64()
        {
            // done by titi, no idea if this is correct
            ulong res = (ulong)(bytes[currentPos] | (bytes[currentPos + 1] << 8) | (bytes[currentPos + 2] << 16) | (bytes[currentPos + 3] << 24)
                            | (bytes[currentPos + 4] << 32) | (bytes[currentPos + 5] << 40) | (bytes[currentPos + 6] << 48) | (bytes[currentPos + 7] << 56));
            currentPos += 8;
            return res;
        }

        public MemoryBinaryReader(byte[] bytes, int startPos, int maxLength)
        {
            this.bytes = bytes;
            currentPos = startPos;
            end = startPos + maxLength;
        }
        
        public MemoryBinaryReader(byte[] bytes, int startPos)
        {
            this.bytes = bytes;
            currentPos = startPos;
            end = bytes.Length;
        }
        
        public MemoryBinaryReader(PooledArray<byte> bytes)
        {
            this.bytes = bytes.AsArray();
            currentPos = 0;
            end = bytes.Length;
        }
        
        public MemoryBinaryReader(byte[] bytes)
        {
            this.bytes = bytes;
            currentPos = 0;
            end = bytes.Length;
        }

        internal Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }

        public float ReadFloat()
        {
            float result = BitConverter.ToSingle(bytes, currentPos);
            currentPos += 4;
            return result;
        }

        public byte ReadByte()
        {
            //Debug.Assert(debugArray == null || !debugArray.IsDisposed, "pooled array is disposed!!! That's so bad");
            return bytes[currentPos++];
        }

        public int Offset
        {
            get => Position;
            set => Seek(value);
        }

        public int Size => end;

        internal void Seek(int offI)
        {
            currentPos = offI;
        }

        public UInt16 ReadUInt16()
        {
            int val = bytes[currentPos] | (bytes[currentPos + 1] << 8);
            currentPos += 2;
            return (UInt16)val;
        }
    }
}
