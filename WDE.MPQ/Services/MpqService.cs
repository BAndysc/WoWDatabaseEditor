using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.Common;
using WDE.Common.MPQ;
using WDE.Module.Attributes;
using WDE.MPQ.Casc;
using WDE.MPQ.Stormlib;
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
            
            // legion
            if (File.Exists(Path.Join(settings.Path, "Data", "config", "3b", "05", "3b0517b51edbe0b96f6ac5ea7eaaed38")))
            {
                Version = GameFilesVersion.Legion_7_3_5;
                return new CascArchive(settings.Path);
            }
            
            var files = Directory.EnumerateFiles(Path.Join(settings.Path, "Data"), "*.mpq", new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive})
                .Where(x => !x.Contains("-update-"))
                .ToList();
            if (files.Any(x => x.Contains("expansion4.MPQ", StringComparison.InvariantCultureIgnoreCase)))
                Version = GameFilesVersion.Mop_5_4_8;
            else if (files.Any(x => x.Contains("expansion3.MPQ", StringComparison.InvariantCultureIgnoreCase)))
                Version = GameFilesVersion.Cataclysm_4_3_4;
            else if (files.Any(x => x.Contains("lichking.MPQ", StringComparison.InvariantCultureIgnoreCase)))
                Version = GameFilesVersion.Wrath_3_3_5a;

            files.Sort(Version == GameFilesVersion.Wrath_3_3_5a ? MpqWrathSortingFunc : (Version == GameFilesVersion.Cataclysm_4_3_4 ? MpqCataSortingFunc : MpqMopSortingFunc));
            List<IMpqArchive> archives = new();
            foreach (var file in files)
            {
                if (file.Contains("Data/Cache") || file.Contains("Data\\Cache"))
                {
                    LOG.LogInformation("Skipping " + file);
                    continue;
                }
                try
                {
                    var archive = settings.OpenType == MpqOpenType.Stormlib ? new StormArchive(file) : MpqArchive.Open(file);
                    archives.Add(archive);
                }
                catch (Exception e)
                {
                    throw new Exception("Couldn't open MPQ file " + Path.GetFileName(file) + ": " + e.Message, e);
                }
            }
            return new MpqArchiveSet(archives.ToArray());
        }

        private int GetWrathOrderFunc(string filename)
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
        
        private int GetCataOrderFunc(string filename)
        {
            if (filename.Contains("expansion3"))
                return 18;
            
            if (filename.Contains("expansion2"))
                return 19;
            
            if (filename.Contains("expansion1"))
                return 20;

            if (filename.Contains("art"))
                return 30;
            
            if (filename.Contains("world"))
                return 31;
            
            if (filename.Contains("world"))
                return 31;

            return 40;
        }
        
        private int GetMopOrderFunc(string filename)
        {
            if (filename.Contains("expansion4"))
                return 17;
            
            if (filename.Contains("expansion3"))
                return 18;
            
            if (filename.Contains("expansion2"))
                return 19;
            
            if (filename.Contains("expansion1"))
                return 20;

            if (filename.Contains("art"))
                return 30;
            
            if (filename.Contains("world2"))
                return 31;
            
            if (filename.Contains("world"))
                return 31;

            return 40;
        }

        private int MpqWrathSortingFunc(string x, string y)
        {
            var a = Path.GetFileNameWithoutExtension(x).ToLowerInvariant();
            var b = Path.GetFileNameWithoutExtension(y).ToLowerInvariant();
            return GetWrathOrderFunc(a).CompareTo(GetWrathOrderFunc(b));
        }
        
        private int MpqCataSortingFunc(string x, string y)
        {
            var a = Path.GetFileNameWithoutExtension(x).ToLowerInvariant();
            var b = Path.GetFileNameWithoutExtension(y).ToLowerInvariant();
            return GetCataOrderFunc(a).CompareTo(GetCataOrderFunc(b));
        }
        
        private int MpqMopSortingFunc(string x, string y)
        {
            var a = Path.GetFileNameWithoutExtension(x).ToLowerInvariant();
            var b = Path.GetFileNameWithoutExtension(y).ToLowerInvariant();
            return GetMopOrderFunc(a).CompareTo(GetMopOrderFunc(b));
        }

        public GameFilesVersion? Version { get; private set; }
        
        private class EmptyMpqArchive : IMpqArchive
        {
            public void Dispose() { }
            public IList<string> KnownFiles => new List<string>();
            public int? ReadFile(byte[] buffer, int size, string path) => null;
            public int? GetFileSize(string path) => null;
            public IMpqArchiveDetails Details => null!;
            public IMpqUserDataHeader UserDataHeader => null!;
            public IMpqArchive Clone() => new EmptyMpqArchive();

            public MpqLibrary Library => MpqLibrary.Managed;
        }
    }
}
