using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.ThemeChanger.Providers;

namespace WDE.ThemeChanger
{
    [AutoRegister, SingleInstance]
    public class ThemeManager : IThemeManager
    {
        private Theme _currentTheme;
        private List<Theme> _themes => new List<Theme>() { new Theme("DarkTheme"), new Theme("BlueTheme") };

        private Theme _defaultTheme = new Theme("BlueTheme");

        public Theme CurrentTheme => _currentTheme;

        public IEnumerable<Theme> Themes => _themes;

        public ThemeManager(IThemeSettingsProvider themeSettings)
        {
            var currentThemeName = themeSettings.GetSettings().Name;

            var theme = new Theme(currentThemeName);

            if (!IsValidTheme(theme))
                theme = _defaultTheme;

            SetTheme(theme);
        }

        private bool IsValidTheme(Theme theme)
        {
            return !string.IsNullOrEmpty(theme.Name) && _themes.Select(t => t.Name).Contains(theme.Name);
        }

        public void SetTheme(Theme theme)
        {
            if (!IsValidTheme(theme))
                return;

            string curentTheme = Application.Current.Resources.MergedDictionaries[1].Source.ToString();
            string compareStr = "/" + theme.Name + ".xaml";

            if (!curentTheme.Contains(compareStr))
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Uri uriOne = new Uri("pack://application:,,,/Xceed.Wpf.AvalonDock.Themes.VS2013;component/" + theme.Name + ".xaml");
                Uri uriTwo = new Uri("Themes/" + theme.Name + ".xaml", UriKind.RelativeOrAbsolute);
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uriOne });
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uriTwo });
            }

            _currentTheme = theme;
        }
    }
}
