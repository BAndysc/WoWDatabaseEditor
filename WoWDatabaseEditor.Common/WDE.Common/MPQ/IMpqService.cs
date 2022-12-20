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
        Wrath_3_3_5a = 335,
        Cataclysm_4_3_4 = 434,
        Mop_5_4_8 = 548,
        Legion_7_3_5 = 735
    }
}
