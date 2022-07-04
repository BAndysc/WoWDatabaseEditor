using System;
using System.Runtime.CompilerServices;
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

        public ReadOnlyMemory<byte> ReadBytes(int length)
        {
            var result = bytes.AsMemory(currentPos, length);
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
            unsafe 
            {
                fixed (byte* ptr = bytes.AsSpan(currentPos, 4))
                {
                    var result = *(int*)ptr;
                    currentPos += 4;
                    return result;
                }
            }
        }

        public uint ReadUInt32()
        {
            unsafe 
            {
                fixed (byte* ptr = bytes.AsSpan(currentPos, 4))
                {
                    var result = *(uint*)ptr;
                    currentPos += 4;
                    return result;
                }
            }
        }

        public ulong ReadUInt64()
        {
            unsafe 
            {
                fixed (byte* ptr = bytes.AsSpan(currentPos, 8))
                {
                    var result = *(ulong*)ptr;
                    currentPos += 8;
                    return result;
                }
            }
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
            unsafe 
            {
                fixed (byte* pBuffer = bytes.AsSpan(currentPos, 12))
                {
                    float* pSample = (float*)pBuffer;
                    var x = pSample[0];
                    var y = pSample[1];
                    var z = pSample[2];
                    currentPos += 4;
                    return new Vector3(x, y, z);
                }
            }
        }

        public float ReadFloat()
        {
            unsafe 
            {
                fixed (byte* pBuffer = bytes.AsSpan(currentPos, 4))
                {
                    float* pSample = (float*)pBuffer;
                    var result = *pSample;
                    currentPos += 4;
                    return result;
                }
            }
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
