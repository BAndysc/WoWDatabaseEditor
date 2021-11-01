using System;
using System.Collections.Generic;

namespace WDE.Common.MPQ
{
    public interface IMpqArchive : IDisposable
    {
        IList<string> KnownFiles { get; }

        int? ReadFile(byte[] buffer, string path);
        int? GetFileSize(string path);

        IMpqArchiveDetails Details { get; }
        IMpqUserDataHeader UserDataHeader { get; }
    }
}