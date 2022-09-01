using WDE.Module.Attributes;

namespace WDE.Common.MPQ
{
    [UniqueProvider]
    public interface IMpqService
    {
        bool IsConfigured();
        IMpqArchive Open();
        GameFilesVersion? Version { get; }
    }
    
    public enum GameFilesVersion
    {
        Wrath_3_3_5a = 335
    }
}
