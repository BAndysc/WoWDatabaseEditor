using WDE.Module.Attributes;

namespace WDE.Common.MPQ
{
    [UniqueProvider]
    public interface IMpqService
    {
        bool IsConfigured();
        IMpqArchive Open();
    }
}