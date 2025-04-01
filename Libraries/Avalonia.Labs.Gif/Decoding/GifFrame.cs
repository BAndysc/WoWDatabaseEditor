using System;

namespace Avalonia.Labs.Gif.Decoding;

internal class GifFrame
{
    public bool HasTransparency, IsInterlaced, IsLocalColorTableUsed;
    public byte TransparentColorIndex;
    public int LzwMinCodeSize, LocalColorTableSize;
    public long LzwStreamPosition;
    public TimeSpan FrameDelay;
    public FrameDisposal FrameDisposalMethod;
    public bool ShouldBackup;
    public GifRect Dimensions;
    public GifColor[]? LocalColorTable;
}
