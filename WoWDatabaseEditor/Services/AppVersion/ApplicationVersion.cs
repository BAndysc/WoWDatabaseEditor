using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.AppVersion
{
    [AutoRegister]
    [SingleInstance]
    public class ApplicationVersion : IApplicationVersion
    {
        public ApplicationVersion(IApplicationReleaseConfiguration appData)
        {
            var branch = appData.GetString("BRANCH");
            var commit = appData.GetString("COMMIT");
            var version = appData.GetInt("BUILD_VERSION");

            VersionKnown = branch != null && commit != null && version != null;
            Branch = branch ?? "<no build data>";
            CommitHash = commit ?? "<no build data>";
            BuildVersion = version ?? 0;
        }
        
        public int BuildVersion { get; }
        public string Branch { get; }
        public string CommitHash { get; }
        public bool VersionKnown { get; }
    }
}