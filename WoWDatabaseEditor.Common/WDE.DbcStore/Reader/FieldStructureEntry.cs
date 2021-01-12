using System;

namespace WDBXEditor.Reader
{
    public class FieldStructureEntry
    {
        public short Bits;
        public byte CommonDataType;
        public int Length = 1;
        public ushort Offset;


        public FieldStructureEntry(short bits, ushort offset, byte commondatatype = 0xFF)
        {
            Bits = bits;
            Offset = offset;
            CommonDataType = commondatatype;
        }

        public bool CommonDataColumn => CommonDataType != 0xFF;

        public int ByteCount
        {
            get
            {
                int value = (32 - Bits) >> 3;
                return value < 0 ? Math.Abs(value) + 4 : value;
            }
        }

        public int BitCount
        {
            get
            {
                int bitSize = 32 - Bits;
                if (bitSize < 0)
                    bitSize = bitSize * -1 + 32;
                return bitSize;
            }
        }

        public void SetLength(FieldStructureEntry nextField)
        {
            Length = Math.Max(1, (int) Math.Floor((nextField.Offset - Offset) / (double) ByteCount));
        }
    }
}