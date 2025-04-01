using System;
using System.IO;
using Avalonia.Animation;
using Avalonia.Media;

namespace Avalonia.Labs.Gif;

internal record struct GifDrawPayload(
    HandlerCommand HandlerCommand,
    Stream? Source = default,
    Size? GifSize = default,
    Size? Size = default,
    Stretch? Stretch = default,
    StretchDirection? StretchDirection = default,
    IterationCount? IterationCount = default);
