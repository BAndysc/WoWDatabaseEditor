using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.MPQ;

namespace WDE.MPQ
{
    public class MpqArchiveSet : IMpqArchive
    {
        private readonly IMpqArchive[] list;

        public MpqArchiveSet(IMpqArchive[] list)
        {
            this.list = list;
        }
    
        public void Dispose()
        {
            foreach (var l in list)
                l.Dispose();
        }

        public IList<string> KnownFiles => list.SelectMany(l => l.KnownFiles).ToList();
    
        public int? ReadFile(byte[] buffer, string path)
        {
            foreach (var l in list)
            {
                try
                {
                    var bytes = l.ReadFile(buffer, path);
                    if (bytes != null)
                        return bytes;
                }
                catch (Exception _)
                {
                }
            }
            return null;
        }

        public int? GetFileSize(string path)
        {
            foreach (var l in list)
            {
                try
                {
                    var bytes = l.GetFileSize(path);
                    if (bytes != null)
                        return bytes;
                }
                catch (Exception _)
                {
                }
            }
            return null;
        }

        public IMpqArchiveDetails Details { get; set; } = null!;
        public IMpqUserDataHeader UserDataHeader { get; set; } = null!;
    }
}