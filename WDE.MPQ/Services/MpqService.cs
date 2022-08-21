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
            var files = Directory.EnumerateFiles(Path.Join(settings.Path, "Data"), "*.mpq", new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive})
                .Where(x => !x.Contains("-update-"))
                .ToList();
            files.Sort(MpqSortingFunc);
            List<IMpqArchive> archives = new();
            foreach (var file in files)
            {
                try
                {
                    var archive = MpqArchive.Open(file);
                    archives.Add(archive);
                }
                catch (Exception e)
                {
                    throw new Exception("Couldn't open MPQ file " + Path.GetFileName(file) + ": " + e.Message, e);
                }
            }
            return new MpqArchiveSet(archives.ToArray());
        }


        private int GetOrderFunc(string filename)
        {
            if (filename.Contains("patch"))
            {
                int num = filename.FirstOrDefault(char.IsNumber);
                return 10 - num;
            }

            if (filename.Contains("lichking"))
                return 11;

            if (filename.Contains("expansion"))
                return 20;

            return 30;
        }

        private int MpqSortingFunc(string x, string y)
        {
            var a = Path.GetFileNameWithoutExtension(x).ToLowerInvariant();
            var b = Path.GetFileNameWithoutExtension(y).ToLowerInvariant();
            return GetOrderFunc(a).CompareTo(GetOrderFunc(b));
        }

        private class EmptyMpqArchive : IMpqArchive
        {
            public void Dispose() { }
            public IList<string> KnownFiles => new List<string>();
            public int? ReadFile(byte[] buffer, string path) => null;
            public int? GetFileSize(string path) => null;
            public IMpqArchiveDetails Details => null!;
            public IMpqUserDataHeader UserDataHeader => null!;
            public IMpqArchive Clone() => new EmptyMpqArchive();
        }
    }
}
