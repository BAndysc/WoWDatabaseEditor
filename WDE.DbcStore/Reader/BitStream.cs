using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDBXEditor.Reader
{
	public class BitStream : IDisposable
	{
		private byte currentByte;
		private long offset;
		private int bit;
		private Stream stream;
		private Encoding encoding = Encoding.UTF8;
		private readonly bool canWrite = true;

		public long Length => stream.Length;
		public int BitPosition => bit;
		public long Offset => offset;


		public BitStream(int capacity = 0)
		{
			this.stream = new MemoryStream(capacity);
			offset = bit = 0;
			canWrite = true;
			currentByte = 0;
		}

		public BitStream(byte[] buffer)
		{
			this.stream = new MemoryStream(buffer);
			offset = bit = 0;
			canWrite = false;
			currentByte = buffer[0];
		}


		#region Methods		
		public void Seek(long offset, int bit)
		{
			if (offset > Length)
			{
				this.offset = Length;
			}
			else
			{
				if (offset >= 0)
				{
					this.offset = offset;
				}
				else
				{
					offset = 0;
				}
			}

			if (bit >= 8)
			{
				this.offset += bit / 8;
				this.bit = bit % 8;
			}
			else
			{
				this.bit = bit;
			}

			UpdateCurrentByte();
		}

		public bool AdvanceBit()
		{
			bit = (bit + 1) % 8;
			if (bit == 0)
			{
				offset++;

				if (canWrite)
					stream.WriteByte(currentByte);

				UpdateCurrentByte();

				return true;
			}

			return false;
		}

		public void SeekNextOffset()
		{
			if (bit != 0)
				WriteByte(0, 8 - bit % 8);
		}

		public byte[] GetStreamData()
		{
			stream.Position = 0;
			MemoryStream s = new MemoryStream();
			stream.CopyTo(s);
			Seek(offset, bit);
			return s.ToArray();
		}

		public bool ChangeLength(long length)
		{
			if (stream.CanSeek && stream.CanWrite)
			{
				stream.SetLength(length);
				return true;
			}
			else
			{
				return false;
			}
		}

		public void CopyStreamTo(Stream stream)
		{
			Seek(0, 0);
			this.stream.CopyTo(stream);
		}

		public MemoryStream CloneAsMemoryStream() => new MemoryStream(GetStreamData());

		#endregion

		#region Bit Read/Write

		private void UpdateCurrentByte()
		{
			stream.Position = offset;

			if (canWrite)
			{
				currentByte = 0;
			}
			else
			{
				currentByte = (byte)stream.ReadByte();
				stream.Position = offset;
			}
		}

		private Bit ReadBit()
		{
			if (offset >= stream.Length)
				throw new IOException("Cannot read in an offset bigger than the length of the stream");

			byte value = (byte)((currentByte >> (bit)) & 1);
			AdvanceBit();

			return value;
		}

		private void WriteBit(Bit data)
		{
			currentByte &= (byte)~(1 << bit);
			currentByte |= (byte)(data << bit);

			if (offset >= stream.Length)
			{
				if (!canWrite || !ChangeLength(Length + (offset - Length) + 1))
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

			byteLength = (byteLength == 0 ? (length + 7) / 8 : byteLength);

			byte[] data = new byte[byteLength];
			for (long i = 0; i < length;)
			{
				byte value = 0;
				for (int p = 0; p < 8 && i < length; i++, p++)
					value |= (byte)(ReadBit() << p);

				data[((i + 7) / 8) - 1] = value;
			}

			return data;
		}

		public byte[] ReadBytesPadded(long length)
		{
			int requiredSize = NextPow2((int)(length + 7) / 8);
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
			short value = BitConverter.ToInt16(ReadBytes(16), 0);
			return value;
		}

		public int ReadInt32(int bitWidth = 32)
		{
			if(bitWidth == 32)
				return BitConverter.ToInt32(ReadBytes(32), 0);

			bitWidth = Math.Min(Math.Max(bitWidth, 0), 32); // clamp values
			byte[] data = ReadBytes(bitWidth, false, 4);
			return BitConverter.ToInt32(data, 0);
		}

		public long ReadInt64()
		{
			long value = BitConverter.ToInt64(ReadBytes(64), 0);
			return value;
		}

		public ushort ReadUInt16()
		{
			ushort value = BitConverter.ToUInt16(ReadBytes(16), 0);
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
			ulong value = BitConverter.ToUInt64(ReadBytes(64), 0);
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
			int position = 0;
			for (long i = 0; i < length;)
			{
				byte value = 0;
				for (int p = 0; p < 8 && i < length; i++, p++)
				{
					value = (byte)((data[position] >> p) & 1);
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
			WriteBits(new byte[] { value }, bits);
		}

		public void WriteChar(char value)
		{
			byte[] bytes = encoding.GetBytes(new char[] { value }, 0, 1);
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

		public void Dispose()
		{
			((IDisposable)stream)?.Dispose();
		}

		internal struct Bit
		{
			private byte value;

			public Bit(int value)
			{
				this.value = (byte)(value & 1);
			}

			public static implicit operator Bit(int value) => new Bit(value);

			public static implicit operator byte(Bit bit) => bit.value;
		}

	}
}
