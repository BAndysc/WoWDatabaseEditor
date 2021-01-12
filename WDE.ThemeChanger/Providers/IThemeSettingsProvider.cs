using WDE.Common.Managers;
using WDE.ThemeChanger.Data;

namespace WDE.ThemeChanger.Providers
{
    public interface IThemeSettingsProvider
    {
        ThemeSettings GetSettings();
        void UpdateSettings(Theme themeName);
    }
}