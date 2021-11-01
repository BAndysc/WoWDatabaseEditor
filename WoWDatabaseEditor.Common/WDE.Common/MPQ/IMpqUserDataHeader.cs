namespace WDE.Common.MPQ
{
    public interface IMpqUserDataHeader
    {
        int ArchiveOffset { get; }
        byte[]? UserData { get; }
        int UserDataReservedSize { get; }
        int UserDataSize { get; }
    }
}