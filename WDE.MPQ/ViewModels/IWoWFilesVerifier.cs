namespace WDE.MPQ.ViewModels
{
    public enum WoWFilesType
    {
        Invalid,
        Wrath,
        Cata
    }
    
    public interface IWoWFilesVerifier
    {
        WoWFilesType VerifyFolder(string? wowFolder);
    }
}