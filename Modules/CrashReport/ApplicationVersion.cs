using WDE.Common.Services;

namespace CrashReport;

public class ApplicationVersion : IApplicationVersion
{
    public ApplicationVersion(IApplicationReleaseConfiguration appData)
    {
        var branch = appData.GetString("BRANCH");
        var commit = appData.GetString("COMMIT");
        var version = appData.GetInt("BUILD_VERSION");
        var marketplace = appData.GetString("MARKETPLACE");
        var platform = appData.GetString("PLATFORM");

        VersionKnown = branch != null && commit != null && version != null;
        Branch = branch ?? "<no build data>";
        CommitHash = commit ?? "<no build data>";
        Marketplace = marketplace ?? "<no build data>";
        Platform = platform ?? "<no build data>";
        BuildVersion = version ?? 0;
    }
        
    public int BuildVersion { get; }
    public string Branch { get; }
    public string CommitHash { get; }
    public string Marketplace { get; }
    public string Platform { get; }
    public bool VersionKnown { get; }
}