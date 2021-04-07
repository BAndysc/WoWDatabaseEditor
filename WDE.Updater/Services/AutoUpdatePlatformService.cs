using System.IO;
using System.Runtime.InteropServices;
using WDE.Module.Attributes;

namespace WDE.Updater.Services
{
    [SingleInstance]
    [AutoRegister]
    public class AutoUpdatePlatformService : IAutoUpdatePlatformService
    {
        public bool PlatformSupportsSelfInstall => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public string UpdateZipFilePath => PlatformSupportsSelfInstall ? "update.zip" : "~/update.zip";
    }
    
    public interface IAutoUpdatePlatformService
    {
        bool PlatformSupportsSelfInstall { get; }
        string UpdateZipFilePath { get; }
    }
}