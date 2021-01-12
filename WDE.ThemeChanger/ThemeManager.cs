using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.ThemeChanger.Providers;

namespace WDE.ThemeChanger
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeManager : IThemeManager
    {
        private readonly Theme defaultTheme = new("BlueTheme");

        public ThemeManager(IThemeSettingsProvider themeSettings)
        {
            string currentThemeName = themeSettings.GetSettings().Name;

            Theme theme = new(currentThemeName);

            if (!IsValidTheme(theme))
                theme = defaultTheme;

            SetTheme(theme);
        }

        private List<Theme> themes => new() {new("DarkTheme"), new("BlueTheme")};

        public Theme CurrentTheme { get; private set; }

        public IEnumerable<Theme> Themes => themes;

        public void SetTheme(Theme theme)
        {
            if (!IsValidTheme(theme))
                return;

            var curentTheme = Application.Current.Resources.MergedDictionaries[1].Source.ToString();
            string compareStr = "/" + theme.Name + ".xaml";

            if (!curentTheme.Contains(compareStr))
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Uri uriOne = new("pack://application:,,,/AvalonDock.Themes.VS2013;component/" + theme.Name + ".xaml");
                Uri uriTwo = new("Themes/" + theme.Name + ".xaml", UriKind.RelativeOrAbsolute);
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {Source = uriOne});
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {Source = uriTwo});
            }

            CurrentTheme = theme;
        }

        private bool IsValidTheme(Theme theme)
        {
            return !string.IsNullOrEmpty(theme.Name) && themes.Select(t => t.Name).Contains(theme.Name);
        }
    }
}