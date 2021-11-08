using System.IO;
using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [UniqueProvider]
    [AutoRegister]
    internal class WoWFilesVerifier : IWoWFilesVerifier
    {
        private string[] wrathMpqs => new string[]{"common.MPQ", "common-2.MPQ", "expansion.MPQ", "lichking.MPQ"};
        private string[] cataMpqs => new string[]
            { "art.MPQ", "expansion1.MPQ", "expansion2.MPQ", "expansion3.MPQ", "world.MPQ", "world2.MPQ" };
        
        public WoWFilesType VerifyFolder(string? wowFolder)
        {
            if (wowFolder == null)
                return WoWFilesType.Invalid;
            
            var folder = new DirectoryInfo(wowFolder);
            var wowFile = new FileInfo(Path.Join(wowFolder, "wow.exe"));
            var dataPath = new DirectoryInfo(Path.Join(wowFolder, "Data"));

            if (!folder.Exists)
                return WoWFilesType.Invalid;

            if (!wowFile.Exists)
                return WoWFilesType.Invalid;
            
            if (!dataPath.Exists)
                return WoWFilesType.Invalid;

            if (ContainsAllFiles(wowFolder, wrathMpqs))
                return WoWFilesType.Wrath;
            
            if (ContainsAllFiles(wowFolder, cataMpqs))
                return WoWFilesType.Cata;

            return WoWFilesType.Invalid;
        }

        private bool ContainsAllFiles(string wowFolder, string[] files)
        {
            foreach (var mpq in files)
            {
                var path = new FileInfo(Path.Join(wowFolder, "Data", mpq));
                if (!path.Exists)
                    return false;
            }

            return true;
        }
    }
}