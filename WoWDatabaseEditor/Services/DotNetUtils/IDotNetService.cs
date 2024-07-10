using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.Processes;

namespace WoWDatabaseEditorCore.Services.DotNetUtils
{
    [UniqueProvider]
    public interface IDotNetService
    {
        Task<bool> IsDotNet8Installed();
        Uri DownloadDotNet8Link { get; }
    }

    [AutoRegister]
    [SingleInstance]
    public class DotNetService : IDotNetService
    {
        private readonly IProcessService processService;
        private readonly IProgramFinder programFinder;

        public DotNetService(IProcessService processService,
            IProgramFinder programFinder)
        {
            this.processService = processService;
            this.programFinder = programFinder;
        }

        public async Task<bool> IsDotNet8Installed()
        {
            try
            {
                var dotnetPath = "dotnet";
                programFinder.TryLocate("dotnet", "dotnet/dotnet.exe");
                var versions = await processService.RunAndGetOutput(dotnetPath, new[]{"--list-runtimes"}, null);
                if (versions == null)
                    return true;
                var runtimes = versions.Split('\n');

                return runtimes.Any(r => r.StartsWith("Microsoft.NETCore.App 8."));
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                return true;
            }
        }

        private Uri GetLink(OSPlatform os, Architecture arch)
        {
            var stringOs = os == OSPlatform.Windows ? "windows" : (os == OSPlatform.OSX ? "macos" : "linux");
            var version = "8.0.2";
            return new Uri($"https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-{version}-{stringOs}-{arch.ToString().ToLower()}-installer");
        }
        
        public Uri DownloadDotNet8Link
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return new Uri("https://learn.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website");
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return GetLink(OSPlatform.OSX, RuntimeInformation.OSArchitecture);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetLink(OSPlatform.Windows, RuntimeInformation.OSArchitecture);

                throw new Exception($"Your OS is not supported by .net 8.0");
            }
        }
    }
}