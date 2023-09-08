using System.Diagnostics;
using WDE.Common.MPQ;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader
{
    public static class Extensions
    {
        public static byte[]? ReadFile(this IMpqArchive archive, FileId path)
        {
            var size = archive.GetFileSize(path.ToString());
            if (!size.HasValue)
                return null;

            var buf = new byte[size.Value];
            archive.ReadFile(buf, (int)size.Value, path.ToString());
            return buf;
        }

        public static PooledArray<byte>? ReadFilePool(this IMpqArchive archive, FileId path, int? maxReadBytes = null)
        {
            var size = archive.GetFileSize(path.ToString());
            if (!size.HasValue)
                return null;

            if (maxReadBytes.HasValue)
                size = Math.Min(size.Value, maxReadBytes.Value);

            var buf = new PooledArray<byte>(size.Value);
            int? read = archive.ReadFile(buf.AsArray(),  size.Value, path.ToString());
            if (read != size)
            {
                buf.Dispose();
                throw new Exception("Couldn't load file " + path);
            }
            return buf;
        }
    }
}