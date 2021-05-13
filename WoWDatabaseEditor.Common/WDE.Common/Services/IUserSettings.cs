using WDE.Common.Annotations;

namespace WDE.Common.Services
{
    public interface IUserSettings
    {
        T? Get<T>(T? defaultValue = default) where T : ISettings;
        void Update<T>(T newSettings) where T : ISettings;
    }

    public interface ISettings
    {
        
    }
}