using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDBXEditor.Storage;

namespace WDBXEditor.Reader.FileTypes
{
    public class HTFX : DBHeader
    {
        public int Build { get; set; }
        public byte[] Hashes { get; set; }
        public List<HotfixEntry> Entries { get; } = new();

        public override bool CheckRecordCount => false;
        public override bool CheckRecordSize => false;
        public override bool CheckTableStructure => false;

        public WDB6 WDB6CounterPart { get; private set; }

        public override void ReadHeader(ref BinaryReader dbReader, string signature)
        {
            Signature = signature;
            Locale = dbReader.ReadInt32();
            Build = dbReader.ReadInt32();

            string tempHeader = dbReader.ReadString(4);
            dbReader.BaseStream.Position -= 4;

            if (tempHeader != "XFTH")
                Hashes = dbReader.ReadBytes(32);

            while (dbReader.BaseStream.Position < dbReader.BaseStream.Length)
                Entries.Add(new HotfixEntry(dbReader));

            Entries.RemoveAll(x => x.IsValid != 1); //Remove old hotfix entries
        }

        public bool HasEntry(DBHeader counterpart)
        {
            return Entries.Any(x =>
                (x.Locale == counterpart.Locale || x.Locale == 0) && x.TableHash == counterpart.TableHash && x.IsValid == 1);
        }

        public bool Read(DBHeader counterpart, DBEntry dbentry)
        {
            WDB6CounterPart = counterpart as WDB6;
            if (WDB6CounterPart == null)
                return false;

            var entries = Entries.Where(x =>
                (x.Locale == counterpart.Locale || x.Locale == 0) && x.TableHash == counterpart.TableHash);
            if (entries.Any())
            {
                OffsetLengths = entries.Select(x => (int) x.Size + 4).ToArray();
                TableStructure = WDB6CounterPart.TableStructure;
                Flags = WDB6CounterPart.Flags;
                FieldStructure = WDB6CounterPart.FieldStructure;
                RecordCount = (uint) entries.Count();

                dbentry.LoadTableStructure();

                IEnumerable<byte> Data = new byte[0];
                foreach (HotfixEntry e in entries)
                    Data = Data.Concat(BitConverter.GetBytes(e.RowId)).Concat(e.Data);

                using (MemoryStream ms = new(Data.ToArray()))
                using (BinaryReader br = new(ms))
                {
                    new DBReader().ReadIntoTable(ref dbentry, br, new Dictionary<int, string>());
                }

                return true;
            }

            return false;
        }
    }

    public class HotfixEntry
    {
        public byte[] Data;
        public byte IsValid;
        public uint Locale;
        public byte[] Padding;
        public uint PushId;
        public int RowId;
        public uint Signature;
        public uint Size;
        public uint TableHash;

        public HotfixEntry(BinaryReader br)
        {
            Signature = br.ReadUInt32();
            Locale = br.ReadUInt32();
            PushId = br.ReadUInt32();
            Size = br.ReadUInt32();
            TableHash = br.ReadUInt32();
            RowId = br.ReadInt32();
            IsValid = br.ReadByte();
            Padding = br.ReadBytes(3);

            Data = br.ReadBytes((int) Size);
        }
    }
}