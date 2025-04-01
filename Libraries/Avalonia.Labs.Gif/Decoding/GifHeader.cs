// Licensed under the MIT License.
// Copyright (C) 2018 Jumar A. Macato, All Rights Reserved.

namespace Avalonia.Labs.Gif.Decoding;

internal class GifHeader
{
    public long HeaderSize;
    internal int Iterations = -1;
    public GifRepeatBehavior? IterationCount;
    public GifRect Dimensions;
    public GifColor[]? GlobalColorTable;
}
