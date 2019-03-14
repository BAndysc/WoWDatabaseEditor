using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDBXEditor.Storage;
using System.Data;
using System.Diagnostics;

namespace WDBXEditor.Reader.FileTypes
{
    public class WDB2 : DBHeader
    {
        public int Build { get; set; }
        public int TimeStamp { get; set; }
        public int[] IndexMap { get; set; } //Maps index to row for all indicies between min and max
        public short[] StringLengths { get; set; } //Length of each string including the 0 byte character

        public override bool ExtendedStringTable => Build > 18273; //WoD has two null bytes

        public override void ReadHeader(ref BinaryReader dbReader, string signature)
        {
            base.ReadHeader(ref dbReader, signature);

            TableHash = dbReader.ReadUInt32();
            Build = dbReader.ReadInt32();
            TimeStamp = dbReader.ReadInt32();
            MinId = dbReader.ReadInt32();
            MaxId = dbReader.ReadInt32();
            Locale = dbReader.ReadInt32();
            CopyTableSize = dbReader.ReadInt32();

            if (MaxId != 0 && Build > 12880)
            {
                int diff = MaxId - MinId + 1; //Calculate the array sizes
                IndexMap = new int[diff];
                StringLengths = new short[diff];

                //Populate the arrays
                for (int i = 0; i < diff; i++)
                    IndexMap[i] = dbReader.ReadInt32();

                for (int i = 0; i < diff; i++)
                    StringLengths[i] = dbReader.ReadInt16();
            }
        }
        
    }
}
