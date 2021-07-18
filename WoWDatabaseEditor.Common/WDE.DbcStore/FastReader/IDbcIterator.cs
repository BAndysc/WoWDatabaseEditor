namespace WDE.DbcStore.FastReader
{
    public interface IDbcIterator
    {
        public int GetInt(int field);
        public uint GetUInt(int field);
        public string GetString(int field);
        public float GetFloat(int field);
    }
}