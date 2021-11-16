using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.Common.MPQ;
using WDE.Module.Attributes;
using WDE.MPQ.ViewModels;

namespace WDE.MPQ.Services
{
    [SingleInstance]
    [AutoRegister]
    public class MpqService : IMpqService
    {
        private readonly IMpqSettings settings;

        public MpqService(IMpqSettings settings)
        {
            this.settings = settings;
        }
        
        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(settings.Path);
        }
        
        public IMpqArchive Open()
        {
            if (settings.Path == null)
                return new EmptyMpqArchive();
            var files = Directory.EnumerateFiles(Path.Join(settings.Path, "Data"), "*.mpq", SearchOption.AllDirectories).ToList();
            return new MpqArchiveSet(files.Select(MpqArchive.Open).ToArray());
        }

        private class EmptyMpqArchive : IMpqArchive
        {
            public void Dispose() { }
            public IList<string> KnownFiles => new List<string>();
            public int? ReadFile(byte[] buffer, string path) => null;
            public int? GetFileSize(string path) => null;
            public IMpqArchiveDetails Details => null!;
            public IMpqUserDataHeader UserDataHeader => null!;
        }
    }
}