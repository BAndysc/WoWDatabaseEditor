using System;
using System.Collections.Generic;
using System.IO;

namespace WDE.Common.MPQ
{
    public interface IMpqArchive : IDisposable
    {
        int? ReadFile(byte[] buffer, int size, string path);
        int? GetFileSize(string path);

        IMpqArchive Clone();
        MpqLibrary Library { get; }
    }

    public enum MpqLibrary
    {
        Managed,
        Stormlib,
        Casclib
    }
}