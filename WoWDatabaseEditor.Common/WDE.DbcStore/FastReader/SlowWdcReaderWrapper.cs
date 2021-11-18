using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using WDBXEditor.Reader;
using WDBXEditor.Storage;
using WDE.Common.DBC;

namespace WDE.DbcStore.FastReader
{
    public class SlowWdcReaderWrapper : IEnumerable<IDbcIterator>
    {
        private readonly string path;

        public SlowWdcReaderWrapper(string path)
        {
            this.path = path;
        }
        
        public IEnumerator<IDbcIterator> GetEnumerator()
        {
            DBReader r = new();
            DBEntry dbEntry = r.Read(path);
            foreach (DataRow row in dbEntry.Data.Rows)
            {
                yield return new Iterator(row);
            }
        }

        private class Iterator : IDbcIterator
        {
            private readonly DataRow row;

            public Iterator(DataRow row)
            {
                this.row = row;
            }

            public int GetInt(int field)
            {
                return Convert.ToInt32(row.ItemArray[field]!.ToString());
            }

            public uint GetUInt(int field)
            {
                return Convert.ToUInt32(row.ItemArray[field]!.ToString());
            }

            public float GetFloat(int field)
            {
                return Convert.ToSingle(row.ItemArray[field]!.ToString());
            }

            public string GetString(int field)
            {
                return row.ItemArray[field]!.ToString() ?? "";
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}