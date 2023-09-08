using System.Runtime.CompilerServices;

namespace WDE.MpqReader.Readers
{
    public interface IBinaryReader
    {
        ReadOnlyMemory<byte> ReadBytes(int length);
        short ReadInt16();
        int ReadInt32();
        ushort ReadUInt16();
        uint ReadUInt32();
        float ReadFloat();
        bool IsFinished();
        byte ReadByte();
        ulong ReadUInt64();
        int Offset { get; set; }
        int Size { get; }
    }

    public static class BinaryReaderExtensions
    {
        public static T ReadStruct<T>(this IBinaryReader reader) where T: unmanaged
        {
            var size = Unsafe.SizeOf<T>();
            var bytes = reader.ReadBytes(size);
            unsafe
            {
                fixed (byte* ptr = bytes.Span)
                {
                    return *(T*)ptr;
                }
            }
        }
    }
}
