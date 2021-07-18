using System;
using System.Collections.Generic;
using System.IO;

namespace WDE.DbcStore.FastReader
{
    public class DatabaseClientFileOpener
    {
        public IEnumerable<IDbcIterator> Open(string path)
        {
            byte[] buffer = new byte[4];
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var bytesRead = fs.Read(buffer, 0, 4);
            fs.Close();

            uint magic = BitConverter.ToUInt32(buffer);
            if (magic == FastDbcReader.WDBC)
                return new FastDbcReader(path);
            
            if (magic == FastDb2Reader.WDB2)
                return new FastDb2Reader(path);

            return new SlowWdcReaderWrapper(path);
        }
    }
}