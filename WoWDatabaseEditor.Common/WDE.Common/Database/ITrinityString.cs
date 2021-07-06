namespace WDE.Common.Database
{
    public interface ITrinityString
    {
        uint Entry { get; }
        string ContentDefault { get; }
        int LocalesCount { get; }
        string? this[int index] { get; } 
    }
}