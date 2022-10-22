using System.Collections;
using System.Collections.Generic;
using DBCD;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader;

public class DbcdWrapper : IWDC
{
    private readonly IDBCDStorage dbcdStorage;

    public DbcdWrapper(IDBCDStorage dbcdStorage)
    {
        this.dbcdStorage = dbcdStorage;
    }
    
    public IEnumerator<IWdcIterator> GetEnumerator()
    {
        Iterator iterator = new Iterator();
        foreach (var row in dbcdStorage.Values)
        {
            iterator.SetRow(row);
            yield return iterator;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public uint RecordCount => (uint)dbcdStorage.Count;
    
    private class Iterator : IWdcIterator
    {
        private DBCDRow row = null!;

        internal void SetRow(DBCDRow row)
        {
            this.row = row;
        }

        public int Id => row.ID;
        
        public byte GetByte(string field) => row.Field<byte>(field);

        public byte GetByte(string field, int index) => (byte)row[field, index];

        public sbyte GetSByte(string field) => row.Field<sbyte>(field);

        public sbyte GetSByte(string field, int index) => (sbyte)row[field, index];

        public int GetInt(string field) => row.Field<int>(field);

        public int GetInt(string field, int index) => (int)row[field, index];
        
        public uint GetUInt(string field) => row.Field<uint>(field);
        
        public uint GetUInt(string field, int index) => (uint)row[field, index];
        
        public short GetShort(string field) => row.Field<short>(field);
        
        public short GetShort(string field, int index) => (short)row[field, index];

        public ushort GetUShort(string field) => row.Field<ushort>(field);

        public ushort GetUShort(string field, int index) => (ushort)row[field, index];
        
        public string GetString(string field) => row.Field<string>(field);
        
        public string GetString(string field, int index) => (string)row[field, index];

        public float GetFloat(string field) => row.Field<float>(field);

        public float GetFloat(string field, int index) => (float)row[field, index];
    }
}