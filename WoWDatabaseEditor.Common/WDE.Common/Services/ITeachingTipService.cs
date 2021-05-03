using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface ITeachingTipService
    {
        bool IsTipShown(string id);
        bool ShowTip(string id);
    }
}