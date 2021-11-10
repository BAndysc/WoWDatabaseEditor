using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.Processes;

namespace WoWDatabaseEditorCore.Services.DotNetUtils
{
    [UniqueProvider]
    public interface IDotNetService
    {
        Task<bool> IsDotNet6Installed();
        Uri DownloadDotNet6Link { get; } 
    }

    [AutoRegister]
    [SingleInstance]
    public class DotNetService : IDotNetService
    {
        private readonly IProcessService processService;

        public DotNetService(IProcessService processService)
        {
            this.processService = processService;
        }

        public async Task<bool> IsDotNet6Installed()
        {
            try
            {
                var dotnetPath = "dotnet";
                var versions = await processService.RunAndGetOutput(dotnetPath, "--list-runtimes", null);
                if (versions == null)
                    return true;
                var runtimes = versions.Split('\n');

                return runtimes.Any(r => r.StartsWith("Microsoft.NETCore.App 6.") ||
                                         r.StartsWith("Microsoft.NETCore.App 7.") ||
                                         r.StartsWith("Microsoft.NETCore.App 8."));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }
        }

        private Uri GetLink(OSPlatform os, Architecture arch)
        {
            var stringOs = os == OSPlatform.Windows ? "windows" : (os == OSPlatform.OSX ? "macos" : "linux");
            var version = "6.0.0";
            return new Uri($"https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-{version}-{stringOs}-{arch.ToString().ToLower()}-installer");
        }
        
        public Uri DownloadDotNet6Link
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return new Uri("https://docs.microsoft.com/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website");
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return GetLink(OSPlatform.OSX, RuntimeInformation.OSArchitecture);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetLink(OSPlatform.Windows, RuntimeInformation.OSArchitecture);

                throw new Exception($"Your OS is not supported by .net 6.0");
            }
        }
    }
}