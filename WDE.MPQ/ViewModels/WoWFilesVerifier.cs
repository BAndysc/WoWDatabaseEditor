using System.IO;
using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [UniqueProvider]
    [AutoRegister]
    internal class WoWFilesVerifier : IWoWFilesVerifier
    {
        private string[] wrathMpqs => new string[]{"common.MPQ", "common-2.MPQ", "expansion.MPQ", "lichking.MPQ"};
        private string[] mopMpqs => new string[]
            { "art.MPQ", "expansion1.MPQ", "expansion2.MPQ", "expansion3.MPQ", "expansion4.MPQ", "world.MPQ", "world2.MPQ" };
        private string[] cataMpqs => new string[]
            { "art.MPQ", "expansion1.MPQ", "expansion2.MPQ", "expansion3.MPQ", "world.MPQ", "world2.MPQ" };
        
        public WoWFilesType VerifyFolder(string? wowFolder)
        {
            if (wowFolder == null)
                return WoWFilesType.Invalid;
            
            var folder = new DirectoryInfo(wowFolder);
            var wowFile = new FileInfo(Path.Join(wowFolder, "wow.exe"));
            var dataPath = new DirectoryInfo(Path.Join(wowFolder, "Data"));
            var macAppPath = new DirectoryInfo(Path.Join(wowFolder, "World of Warcraft.app"));

            if (!folder.Exists)
                return WoWFilesType.Invalid;

            if (!wowFile.Exists && !macAppPath.Exists)
                return WoWFilesType.Invalid;
            
            if (!dataPath.Exists)
                return WoWFilesType.Invalid;

            if (File.Exists(Path.Join(wowFolder, "Data", "config", "3b", "05", "3b0517b51edbe0b96f6ac5ea7eaaed38")))
                return WoWFilesType.Legion;
            
            if (ContainsAllFiles(wowFolder, wrathMpqs))
                return WoWFilesType.Wrath;
            
            if (ContainsAllFiles(wowFolder, mopMpqs))
                return WoWFilesType.Mop;
            
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