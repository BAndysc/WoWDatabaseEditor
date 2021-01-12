using System.Collections.Generic;

namespace WDBXEditor.Reader
{
    public class RelationShipData
    {
        public Dictionary<uint, byte[]> Entries; // index, id
        public uint MaxId;
        public uint MinId;
        public uint Records;
    }
}