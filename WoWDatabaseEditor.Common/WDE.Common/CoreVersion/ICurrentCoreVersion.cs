using WDE.Module.Attributes;

namespace WDE.Common.CoreVersion
{
    [UniqueProvider]
    public interface ICurrentCoreVersion
    {
        ICoreVersion Current { get; }
    }
}