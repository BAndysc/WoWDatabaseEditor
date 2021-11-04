namespace WDE.MpqReader.Readers
{
    public interface IBinaryReader
    {
        byte[] ReadBytes(int length);
        short ReadInt16();
        int ReadInt32();
        ushort ReadUInt16();
        uint ReadUInt32();
        float ReadFloat();
        bool IsFinished();
        byte ReadByte();
        int Offset { get; set; }
        int Size { get; }
    }
}
