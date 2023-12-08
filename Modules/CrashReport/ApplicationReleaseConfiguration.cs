using WDE.Common.Services;

namespace CrashReport;

public class ApplicationReleaseConfiguration : BaseApplicationReleaseConfiguration
{
    public ApplicationReleaseConfiguration(IFileSystem fileSystem) : base(fileSystem)
    {
    }
}