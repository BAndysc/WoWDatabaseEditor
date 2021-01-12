using System;
using System.IO;
using System.Text;

namespace WDBXEditor.Reader
{
    public class BitStream : IDisposable
    {
        private readonly bool canWrite = true;
        private byte currentByte;
        private readonly Encoding encoding = Encoding.UTF8;
        private readonly Stream stream;


        public BitStream(int capacity = 0)
        {
            stream = new MemoryStream(capacity);
            Offset = BitPosition = 0;
            canWrite = true;
            currentByte = 0;
        }

        public BitStream(byte[] buffer)
        {
            stream = new MemoryStream(buffer);
            Offset = BitPosition = 0;
            canWrite = false;
            currentByte = buffer[0];
        }

        public long Length => stream.Length;
        public int BitPosition { get; private set; }

        public long Offset { get; private set; }

        public void Dispose()
        {
            ((IDisposable) stream)?.Dispose();
        }

        internal struct Bit
        {
            private readonly byte value;

            public Bit(int value)
            {
                this.value = (byte) (value & 1);
            }

            public static implicit operator Bit(int value)
            {
                return new(value);
            }

            public static implicit operator byte(Bit bit)
            {
                return bit.value;
            }
        }


        #region Methods

        public void Seek(long offset, int bit)
        {
            if (offset > Length)
                this.Offset = Length;
            else
            {
                if (offset >= 0)
                    this.Offset = offset;
                else
                    offset = 0;
            }

            if (bit >= 8)
            {
                this.Offset += bit / 8;
                this.BitPosition = bit % 8;
            }
            else
                this.BitPosition = bit;

            UpdateCurrentByte();
        }

        public bool AdvanceBit()
        {
            BitPosition = (BitPosition + 1) % 8;
            if (BitPosition == 0)
            {
                Offset++;

                if (canWrite)
                    stream.WriteByte(currentByte);

                UpdateCurrentByte();

                return true;
            }

            return false;
        }

        public void SeekNextOffset()
        {
            if (BitPosition != 0)
                WriteByte(0, 8 - BitPosition % 8);
        }

        public byte[] GetStreamData()
        {
            stream.Position = 0;
            MemoryStream s = new();
            stream.CopyTo(s);
            Seek(Offset, BitPosition);
            return s.ToArray();
        }

        public bool ChangeLength(long length)
        {
            if (stream.CanSeek && stream.CanWrite)
            {
                stream.SetLength(length);
                return true;
            }

            return false;
        }

        public void CopyStreamTo(Stream stream)
        {
            Seek(0, 0);
            this.stream.CopyTo(stream);
        }

        public MemoryStream CloneAsMemoryStream()
        {
            return new(GetStreamData());
        }

        #endregion

        #region Bit Read/Write

        private void UpdateCurrentByte()
        {
            stream.Position = Offset;

            if (canWrite)
                currentByte = 0;
            else
            {
                currentByte = (byte) stream.ReadByte();
                stream.Position = Offset;
            }
        }

        private Bit ReadBit()
        {
            if (Offset >= stream.Length)
                throw new IOException("Cannot read in an offset bigger than the length of the stream");

            var value = (byte) ((currentByte >> BitPosition) & 1);
            AdvanceBit();

            return value;
        }

        private void WriteBit(Bit data)
        {
            currentByte &= (byte) ~(1 << BitPosition);
            currentByte |= (byte) (data << BitPosition);

            if (Offset >= stream.Length)
            {
                if (!canWrite || !ChangeLength(Length + (Offset - Length) + 1))
                    throw new IOException("Attempted to write past the length of the stream.");
            }

            AdvanceBit();
        }

        #endregion

        #region Read

        public byte[] ReadBytes(long length, bool isBytes = false, long byteLength = 0)
        {
            if (isBytes)
                length *= 8;

            byteLength = byteLength == 0 ? (length + 7) / 8 : byteLength;

            var data = new byte[byteLength];
            for (long i = 0; i < length;)
            {
                byte value = 0;
                for (var p = 0; p < 8 && i < length; i++, p++)
                    value |= (byte) (ReadBit() << p);

                data[(i + 7) / 8 - 1] = value;
            }

            return data;
        }

        public byte[] ReadBytesPadded(long length)
        {
            int requiredSize = NextPow2((int) (length + 7) / 8);
            byte[] data = ReadBytes(length, false, requiredSize);
            return data;
        }

        public byte ReadByte()
        {
            return ReadBytes(8)[0];
        }

        public byte ReadByte(int bits)
        {
            bits = Math.Min(Math.Max(bits, 0), 8); // clamp values
            return ReadBytes(bits)[0];
        }

        public string ReadString(int length)
        {
            // UTF8 - revert if encoding gets exposed
            return encoding.GetString(ReadBytes(8 * length));
        }

        public short ReadInt16()
        {
            var value = BitConverter.ToInt16(ReadBytes(16), 0);
            return value;
        }

        public int ReadInt32(int bitWidth = 32)
        {
            if (bitWidth == 32)
                return BitConverter.ToInt32(ReadBytes(32), 0);

            bitWidth = Math.Min(Math.Max(bitWidth, 0), 32); // clamp values
            byte[] data = ReadBytes(bitWidth, false, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public long ReadInt64()
        {
            var value = BitConverter.ToInt64(ReadBytes(64), 0);
            return value;
        }

        public ushort ReadUInt16()
        {
            var value = BitConverter.ToUInt16(ReadBytes(16), 0);
            return value;
        }

        public uint ReadUInt32(int bitWidth = 32)
        {
            if (bitWidth == 32)
                return BitConverter.ToUInt32(ReadBytes(32), 0);

            bitWidth = Math.Min(Math.Max(bitWidth, 0), 32); // clamp values
            byte[] data = ReadBytes(bitWidth, false, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public ulong ReadUInt64()
        {
            var value = BitConverter.ToUInt64(ReadBytes(64), 0);
            return value;
        }


        private int NextPow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return Math.Max(v, 1);
        }

        #endregion

        #region Write

        public void WriteBits(byte[] data, long length)
        {
            var position = 0;
            for (long i = 0; i < length;)
            {
                byte value = 0;
                for (var p = 0; p < 8 && i < length; i++, p++)
                {
                    value = (byte) ((data[position] >> p) & 1);
                    WriteBit(value);
                }

                position++;
            }
        }

        public void WriteBytes(byte[] data, long length)
        {
            WriteBits(data, length * 8);
        }

        public void WriteByte(byte value, int bits = 8)
        {
            bits = Math.Min(Math.Max(bits, 0), 8); // clamp values
            WriteBits(new[] {value}, bits);
        }

        public void WriteChar(char value)
        {
            byte[] bytes = encoding.GetBytes(new[] {value}, 0, 1);
            WriteBits(bytes, bytes.Length * 8);
        }

        public void WriteString(string value)
        {
            byte[] bytes = encoding.GetBytes(value);
            WriteBits(bytes, bytes.Length * 8);
        }

        public void WriteCString(string value)
        {
            byte[] bytes = encoding.GetBytes(value);
            Array.Resize(ref bytes, bytes.Length + 1);
            WriteBits(bytes, bytes.Length * 8);
        }

        public void WriteInt16(short value, int bits = 16)
        {
            bits = Math.Min(Math.Max(bits, 0), 16); // clamp values
            WriteBits(BitConverter.GetBytes(value), bits);
        }

        public void WriteInt32(int value, int bits = 32)
        {
            bits = Math.Min(Math.Max(bits, 0), 32); // clamp values
            WriteBits(BitConverter.GetBytes(value), bits);
        }

        public void WriteInt64(long value, int bits = 64)
        {
            bits = Math.Min(Math.Max(bits, 0), 64); // clamp values
            WriteBits(BitConverter.GetBytes(value), bits);
        }

        public void WriteUInt16(ushort value, int bits = 16)
        {
            bits = Math.Min(Math.Max(bits, 0), 16); // clamp values
            WriteBits(BitConverter.GetBytes(value), bits);
        }

        public void WriteUInt32(uint value, int bits = 32)
        {
            bits = Math.Min(Math.Max(bits, 0), 32); // clamp values
            WriteBits(BitConverter.GetBytes(value), bits);
        }

        public void WriteUInt64(ulong value, int bits = 64)
        {
            bits = Math.Min(Math.Max(bits, 0), 64); // clamp values
            WriteBits(BitConverter.GetBytes(value), bits);
        }

        #endregion
    }
}