using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IConfigureService
    {
        void ShowSettings();
        T? ShowSettings<T>() where T : IConfigurable;
    }
}