// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System.Buffers;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.BZip2;

namespace WDE.MPQ
{
    static class Compression
    {
        public static int BZip2Decompress(byte[] input, int start, int length, byte[] output)
        {
            using var inputStream = new MemoryStream(input, start, length);
            using var outputStream = new MemoryStream(output, true);
            BZip2.Decompress(inputStream, outputStream, false);
            return (int)outputStream.Position;
        }
        
        public static int Deflate(byte[] input, int start, int length, byte[] output)
        {
            // see http://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
            // and possibly http://connect.microsoft.com/VisualStudio/feedback/details/97064/deflatestream-throws-exception-when-inflating-pdf-streams
            // for more info on why we have to skip two extra bytes because of ZLIB
            using var inputStream = new MemoryStream(input, 2 + start, length - 2);
            using var deflate = new DeflateStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream(output, true);
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            var read = deflate.Read(buffer, 0, 1024);

            while (read == buffer.Length)
            {
                outputStream.Write(buffer, 0, read);
                read = deflate.Read(buffer, 0, 1024);
            }

            outputStream.Write(buffer, 0, read);
            ArrayPool<byte>.Shared.Return(buffer);
            return (int)outputStream.Position;
        }
        
        public static byte[] BZip2Decompress(byte[] input, int start, int length)
        {
            using var inputStream = new MemoryStream(input, start, length);
            using var outputStream = new MemoryStream();
            BZip2.Decompress(inputStream, outputStream, false);
            return outputStream.ToArray();
        }

        public static byte[] Deflate(byte[] input, int start, int length)
        {
            // see http://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
            // and possibly http://connect.microsoft.com/VisualStudio/feedback/details/97064/deflatestream-throws-exception-when-inflating-pdf-streams
            // for more info on why we have to skip two extra bytes because of ZLIB
            using var inputStream = new MemoryStream(input, 2 + start, length - 2, false);
            using var deflate = new DeflateStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            var read = deflate.Read(buffer, 0, 1024);

            while (read == buffer.Length)
            {
                outputStream.Write(buffer, 0, read);
                read = deflate.Read(buffer, 0, 1024);
            }

            outputStream.Write(buffer, 0, read);
            ArrayPool<byte>.Shared.Return(buffer);
            return outputStream.ToArray();
        }
    }
}