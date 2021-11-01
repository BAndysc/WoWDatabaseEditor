using System.IO;
using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [UniqueProvider]
    [AutoRegister]
    internal class WoWFilesVerifier : IWoWFilesVerifier
    {
        private string[] requiredMpqs => new string[]{"common.MPQ", "common-2.MPQ", "expansion.MPQ", "lichking.MPQ"};
        
        public bool VerifyFolder(string? wowFolder)
        {
            if (wowFolder == null)
                return false;
            
            var folder = new DirectoryInfo(wowFolder);
            var wowFile = new FileInfo(Path.Join(wowFolder, "wow.exe"));
            var dataPath = new DirectoryInfo(Path.Join(wowFolder, "Data"));

            if (!folder.Exists)
                return false;

            if (!wowFile.Exists)
                return false;
            
            if (!dataPath.Exists)
                return false;

            foreach (var mpq in requiredMpqs)
            {
                var path = new FileInfo(Path.Join(wowFolder, "Data", mpq));
                if (!path.Exists)
                    return false;
            }

            return true;
        }
    }
}