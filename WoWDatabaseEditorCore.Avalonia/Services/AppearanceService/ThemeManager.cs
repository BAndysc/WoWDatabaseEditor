using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaStyles;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeManager : IThemeManager
    {
        private readonly Theme defaultTheme = new(SystemThemeOptions.Auto.ToString());

        public ThemeManager(IThemeSettingsProvider themeSettings)
        {
            string currentThemeName = themeSettings.GetSettings().Name;

            Theme theme = new(currentThemeName);

            if (!IsValidTheme(theme))
                theme = defaultTheme;

            SetTheme(theme);
            if (Enum.TryParse<SystemThemeOptions>(theme.Name, out var t))
                AvaloniaThemeStyle.Theme = t;
        }

        private List<Theme> themes { get; } = Enum.GetNames<SystemThemeOptions>().Select(name => new Theme(name)).ToList();

        public Theme CurrentTheme { get; private set; }

        public IEnumerable<Theme> Themes => themes;

        public void SetTheme(Theme theme)
        {
            if (!IsValidTheme(theme))
                return;

            CurrentTheme = theme;
        }

        private bool IsValidTheme(Theme theme)
        {
            return !string.IsNullOrEmpty(theme.Name) && themes.Select(t => t.Name).Contains(theme.Name);
        }
    }
}