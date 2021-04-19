using System.Diagnostics;
using System.IO;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Updater.Services
{
    [SingleInstance]
    [AutoRegister]
    public class StandaloneUpdater : IStandaloneUpdater
    {
        private readonly IAutoUpdatePlatformService platform;
        private readonly IFileSystem fs;

        public StandaloneUpdater(IAutoUpdatePlatformService platform, IFileSystem fs)
        {
            this.platform = platform;
            this.fs = fs;
        }

        public bool HasPendingUpdate()
        {
            return platform.PlatformSupportsSelfInstall && fs.Exists(platform.UpdateZipFilePath);
        }

        public void RenameIfNeeded()
        {
            if (!platform.PlatformSupportsSelfInstall)
                return;
            
            RenameIfExist("_Updater", "Updater");
            RenameIfExist("_Updater.dll", "Updater.dll");
            RenameIfExist("_Updater.pdb", "Updater.pdb");
            RenameIfExist("_Updater.exe", "Updater.exe");
        }

        private void RenameIfExist(string oldName, string newName)
        {
            if (File.Exists(oldName))
            {
                if (File.Exists(newName))
                    File.Delete(newName);
                File.Move(oldName, newName);
            }
        }

        public bool Launch()
        {
            if (!platform.PlatformSupportsSelfInstall)
                return false;

            if (!TryLaunch("Updater.exe"))
                return TryLaunch("Updater");

            return true;
        }
        
        private bool TryLaunch(string file)
        {
            if (File.Exists(file))
            {
                Process.Start(file);
                return true;
            }

            return false;
        }
    }
}