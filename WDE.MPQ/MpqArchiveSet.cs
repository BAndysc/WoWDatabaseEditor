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
    
        public IMpqArchive Clone()
        {
            return new MpqArchiveSet(list.Select(l => l.Clone()).ToArray());
        }
        
        public void Dispose()
        {
            foreach (var l in list)
                l.Dispose();
        }

        //public IList<string> KnownFiles => list.SelectMany(l => l.KnownFiles).ToList();
    
        public int? ReadFile(byte[] buffer, int size, string path)
        {
            foreach (var l in list)
            {
                try
                {
                    var bytes = l.ReadFile(buffer, size, path);
                    if (bytes != null)
                        return bytes;
                }
                catch (Exception)
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
                catch (Exception)
                {
                }
            }
            return null;
        }

        public IMpqArchiveDetails Details { get; set; } = null!;
        public IMpqUserDataHeader UserDataHeader { get; set; } = null!;
        public MpqLibrary Library => list.Length > 0 ? list[0].Library : MpqLibrary.Managed;
    }
}