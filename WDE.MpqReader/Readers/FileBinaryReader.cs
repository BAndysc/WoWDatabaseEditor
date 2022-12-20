namespace WDE.MpqReader.Readers
{
    public class FileBinaryReader : IBinaryReader
    {
        private readonly FileStream fileStream;
        private readonly BinaryReader binaryReader;

        public ReadOnlyMemory<byte> ReadBytes(int length)
        {
            return binaryReader.ReadBytes(length);
        }

        public int ReadInt32()
        {
            return binaryReader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return binaryReader.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            return binaryReader.ReadUInt64();
        }

        public short ReadInt16()
        {
            return binaryReader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return binaryReader.ReadUInt16();
        }

        public float ReadFloat()
        {
            return binaryReader.ReadSingle();
        }

        public bool IsFinished()
        {
            return binaryReader.BaseStream.Position == binaryReader.BaseStream.Length;
        }

        public byte ReadByte()
        {
            return binaryReader.ReadByte();
        }

        public int Offset
        {
            get => (int)GetPosition();
            set => Seek(value);
        }

        public int Size => (int)binaryReader.BaseStream.Length;

        public Vector3 ReadVector3()
        {
            return new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
        }

        public Vector2 ReadVector2()
        {
            return new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
        }

        public Quaternion ReadQuaterunion()
        {
            return new Quaternion(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
        }
        
        // public FixedPoint_0_15 ReadFixedPoint0_15()
        // {
        //     return new FixedPoint_0_15(binaryReader.ReadInt16());
        // }
        //
        // public VertexProperty ReadVertexProperty()
        // {
        //     return new VertexProperty(binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte());
        // }
        //
        // public CAaBox ReadCAaBox()
        // {
        //     return new CAaBox(ReadVector3(), ReadVector3());
        // }

        // Debug purposes :D
        public long GetPosition()
        {
            return binaryReader.BaseStream.Position;
        }

        public long GetLen()
        {
            return binaryReader.BaseStream.Length;
        }

        public void Seek(int offset)
        {
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public void CloseReader()
        {
            binaryReader.Close();
            fileStream.Close();
        }

        public FileBinaryReader(FileStream fileStream)
        {
            this.fileStream = fileStream;
            this.binaryReader = new BinaryReader(fileStream);
        }
    }
}
