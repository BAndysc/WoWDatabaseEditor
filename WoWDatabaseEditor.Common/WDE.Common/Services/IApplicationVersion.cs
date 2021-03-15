namespace WDE.Common.Services
{
    public interface IApplicationVersion
    {
        int BuildVersion { get; }
        string Branch { get; }
        string CommitHash { get; }
        
        bool VersionKnown { get; }
    }
}