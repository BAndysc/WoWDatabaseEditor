using System.IO;

namespace Avalonia.Labs.Gif
{
    /// <summary>
    /// Defines a source for accessing GIF image data and its pixel dimensions.
    /// </summary>
    /// <remarks>Implementations of this interface provide access to the underlying GIF image stream and its
    /// size. The returned stream from <see cref="GetStream"/> should not be disposed by the caller, as ownership
    /// remains with the source implementation.</remarks>
    public interface IGifSource
    {
        PixelSize Size { get; }

        /// <summary>
        /// Gets a stream containing the source GIF image data.
        /// Note: the returned stream should not be disposed by the caller.
        /// </summary>
        Stream GetStream();
    }
}
