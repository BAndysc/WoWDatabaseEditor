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

        public static int DeflateTo(byte[] from, int start, int length, Span<byte> to)
        {
            using var deflate = new DeflateStream(new NoAllocMemoryStream(from, 2 + start, length - 2), CompressionMode.Decompress);
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            int totalRead = 0;
            int offset = 0;
            while (true)
            {
                var read = deflate.Read(buffer, 0, 1024);
                buffer.AsSpan(0, read).CopyTo(to.Slice(offset));
                offset += read;
                totalRead += read;
                if (read != buffer.Length)
                    break;
            }
            ArrayPool<byte>.Shared.Return(buffer);
            return totalRead;
        }

        private class NoAllocMemoryStream : Stream
        {
            private byte[] array;
            private long startPosition;
            private long position;
            private long endPosition;
            public override void Flush() { }

            public NoAllocMemoryStream(byte[] array, int start, int length)
            {
                this.array = array;
                startPosition = start;
                position = start;
                endPosition = start + length;
            }
            
            public override int Read(byte[] buffer, int offset, int count)
            {
                var left = endPosition - position;
                var toCopyLength = Math.Min(count, left); // never copy more than we have
                Array.Copy(array, position, buffer, offset, toCopyLength);
                return (int)toCopyLength;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        position = startPosition + offset;
                        break;
                    case SeekOrigin.Current:
                        position += offset;
                        break;
                    case SeekOrigin.End:
                        position = endPosition - offset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
                }

                return position;
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => endPosition - startPosition;

            public override long Position
            {
                get => position - startPosition;
                set => position = value + startPosition;
            }
        }
    }
}