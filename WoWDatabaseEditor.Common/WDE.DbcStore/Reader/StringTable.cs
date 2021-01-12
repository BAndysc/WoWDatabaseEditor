using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WDBXEditor.Reader
{
    public class StringTable : IDisposable
    {
        private readonly Dictionary<string, int> stringlookup = new();

        public MemoryStream StringStream = new();
        public Dictionary<int, string> Table = new();

        public StringTable()
        {
        }

        public StringTable(bool extended)
        {
            StringStream.WriteByte(0);
            if (extended)
                StringStream.WriteByte(0);
        }

        public int Size => (int) StringStream.Length;

        public void Dispose()
        {
            ((IDisposable) StringStream).Dispose();
        }

        /// <summary>
        ///     Writes the string into the stringtable and return's it's position.
        ///     <para>If unique then it will check if the string is existent and return the previous position.</para>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="unique"></param>
        /// <returns></returns>
        public int Write(string str, bool duplicates = false, bool stripCr = true)
        {
            if (stripCr)
                str = str.Replace("\r\n", "\n").Replace(Environment.NewLine, "\n");

            var offset = 0;
            if (str == "") //Empty string always 0
                return offset;

            //WDB2 with MaxId allows duplicates else distinct strings
            if (duplicates || !stringlookup.TryGetValue(str, out offset))
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(str);
                offset = (int) StringStream.Position;

                if (!duplicates)
                    stringlookup.Add(str, offset);

                StringStream.Write(strBytes, 0, strBytes.Length);
                StringStream.WriteByte(0);
            }

            return offset;
        }

        public void CopyTo(Stream s)
        {
            StringStream.Position = 0;
            StringStream.CopyTo(s);
        }


        /// <summary>
        ///     Reads the binary data from start to end returning a list of strings and their positions.
        /// </summary>
        /// <param name="dbReader"></param>
        /// <param name="stringTableStart"></param>
        /// <param name="stringTableEnd"></param>
        /// <returns></returns>
        public Dictionary<int, string> Read(BinaryReader dbReader, long stringTableStart, long stringTableEnd, bool absolute = false)
        {
            if (dbReader.BaseStream.Position > stringTableEnd)
                return Table;

            while (dbReader.BaseStream.Position < stringTableEnd)
            {
                var pos = (int) dbReader.BaseStream.Position;
                var index = (int) (pos - stringTableStart);
                Table[absolute ? pos : index] = dbReader.ReadStringNull();
            }

            return Table;
        }

        public Dictionary<int, string> Read(BinaryReader dbReader, long stringTableStart)
        {
            return Read(dbReader, stringTableStart, dbReader.BaseStream.Length);
        }
    }
}