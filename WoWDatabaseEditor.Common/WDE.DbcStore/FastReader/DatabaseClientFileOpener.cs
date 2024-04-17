using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.DBC;
using WDE.Common.Exceptions;
using WDE.Module.Attributes;

namespace WDE.DbcStore.FastReader
{
    [AutoRegister]
    public class DatabaseClientFileOpener : IDatabaseClientFileOpener
    {
        private readonly DBCD.DBCD dbcd;

        public DatabaseClientFileOpener(DBCD.DBCD dbcd)
        {
            this.dbcd = dbcd;
        }
        
        public IDBC Open(byte[] data)
        {
            uint magic = BitConverter.ToUInt32(data);
            if (magic == FastDbcReader.WDBC)
                return new FastDbcReader(data);
            
            if (magic == FastDb2Reader.WDB2)
                return new FastDb2Reader(data);

            throw new Exception("Only dbc and db2 supported");
        }
        
        public IDBC Open(string path)
        {
            try
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

                if (magic == FastWdc1Reader.WDC1)
                    return new FastWdc1Reader(path);
            }
            catch (FileNotFoundException e)
            {
                throw new UserException(e);
            }

            throw new Exception("Unsupported version");
        }

        public IWDC OpenWdc(string table, byte[] data)
        {
            return new DbcdWrapper(dbcd.Load(new MemoryStream(data), table));
        }

        public IWDC OpenWdc(string path)
        {
            byte[] buffer = new byte[4];
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var bytesRead = fs.Read(buffer, 0, 4);
            fs.Close();
            return OpenWdc(path, buffer);
        }
    }
}