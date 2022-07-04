namespace WDE.MPQ.ViewModels
{
    public enum WoWFilesType
    {
        Invalid,
        Wrath,
        Mop,
        Cata,
        Legion
    }
    
    public interface IWoWFilesVerifier
    {
        WoWFilesType VerifyFolder(string? wowFolder);
    }
}