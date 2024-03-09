using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IGameViewService
{
    bool IsSupported { get; }
    void Open();
}