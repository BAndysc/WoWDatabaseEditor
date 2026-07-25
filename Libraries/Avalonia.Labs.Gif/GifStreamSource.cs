using System;
using System.IO;
using System.Threading;
using Avalonia.Labs.Gif.Decoding;
using Avalonia.Platform;

namespace Avalonia.Labs.Gif
{
    public class GifStreamSource : IGifSource, IDisposable
    {
        private readonly Stream stream;

        private readonly PixelSize size;

        private bool disposedValue;

        private GifStreamSource(Stream stream)
        {
            using (GifDecoder decoder = new(stream, CancellationToken.None))
            {
                this.stream = stream;
                this.size = decoder.Size;
            }
        }

        /// <inheritdoc/>
        public PixelSize Size => size;

        /// <inheritdoc/>
        public Stream GetStream()
        {
            stream.Position = 0;
            return stream;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    stream.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <exception cref="InvalidGifStreamException"/>
        public static GifStreamSource FromStream(Stream stream)
        {
            return new GifStreamSource(stream);
        }

        /// <summary>
        /// Load a stream from a resource URI and wrap it as a GIF source.
        /// </summary>
        /// <param name="uri">The resource URI</param>
        /// <param name="baseUri">The base URI to use if uri is relative</param>
        /// <exception cref="InvalidGifStreamException"/>
        public static GifStreamSource FromUri(Uri uri, Uri? baseUri = null)
        {
            return new GifStreamSource(AssetLoader.Open(uri, baseUri));
        }

        /// <summary>
        /// Load a stream from a resource URI and wrap it as a GIF source.
        /// </summary>
        /// <param name="uriString">The absolute resource URI string</param>
        /// <exception cref="InvalidGifStreamException"/>
        /// <exception cref="ArgumentException"/>
        public static GifStreamSource FromUriString(string uriString)
        {
            if (Uri.TryCreate(uriString, UriKind.Absolute, out Uri? uri))
            {
                return new GifStreamSource(AssetLoader.Open(uri));
            }
            else
            {
                throw new ArgumentException("Invalid absolute URI string.", nameof(uriString));
            }
        }
    }
}
