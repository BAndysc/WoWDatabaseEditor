using WDE.Common.MPQ;

namespace WDE.MpqReader
{
    public static class Extensions
    {
        public static byte[]? ReadFile(this IMpqArchive archive, string path)
        {
            var size = archive.GetFileSize(path);
            if (!size.HasValue)
                return null;

            var buf = new byte[size.Value];
            archive.ReadFile(buf, path);
            return buf;
        }

        public static PooledArray<byte>? ReadFilePool(this IMpqArchive archive, string path)
        {
            var size = archive.GetFileSize(path);
            if (!size.HasValue)
                return null;

            var buf = new PooledArray<byte>(size.Value);
            archive.ReadFile(buf.AsArray(), path);
            return buf;
        }
    }
}