namespace WDE.Common.DBC
{
    public interface IDbcIterator
    {
        uint Key { get; }
        public int GetInt(int field);
        public uint GetUInt(int field);
        public uint GetUInt(int field, int index);
        public ushort GetUShort(int field, int index);
        public ushort GetUShort(int field);
        public string GetString(int field);
        public float GetFloat(int field);
    }
}