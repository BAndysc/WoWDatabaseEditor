// Licensed under the MIT License.
// Copyright (C) 2018 Jumar A. Macato, All Rights Reserved.

using System;

namespace Avalonia.Labs.Gif.Decoding;

[Serializable]
internal sealed class InvalidGifStreamException(string message) : Exception(message);
