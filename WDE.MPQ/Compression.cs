// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System.Buffers;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

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
            using var inputStream = new NoAllocMemoryStream(input, start, length);
            using var inflater = new InflaterInputStream(inputStream);
            using var outputStream = new MemoryStream(output, true);
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            var read = inflater.Read(buffer, 0, 1024);

            while (read == buffer.Length)
            {
                outputStream.Write(buffer, 0, read);
                read = inflater.Read(buffer, 0, 1024);
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

        private static ThreadLocal<Inflater> inflaters = new(() => new Inflater());

        public static int DeflateTo(byte[] from, int start, int length, Span<byte> to)
        {
            using var memory = new NoAllocMemoryStream(from, start, length);
            var inf = inflaters.Value!;
            inf.Reset();
            using var inflater = new InflaterInputStream(memory, inf, 1024);
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            int totalRead = 0;
            int offset = 0;
            while (true)
            {
                var read = inflater.Read(buffer, 0, 1024);
                if (read == 0)
                    break;
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
                position += toCopyLength;
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