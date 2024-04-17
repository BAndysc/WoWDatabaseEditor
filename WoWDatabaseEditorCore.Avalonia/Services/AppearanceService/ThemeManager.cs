﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Styling;
using AvaloniaStyles;
using AvaloniaStyles.Utils;
using WDE.Common.Factories.Http;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeManager : IThemeManager, IUserAgentPart
    {
        private readonly Theme defaultTheme = new(SystemThemeOptions.Auto.ToString());

        public ThemeManager(IThemeSettingsProvider themeSettings)
        {
            var settings = themeSettings.GetSettings();
            string currentThemeName = settings.Name;

            if (currentThemeName == "Windows10Dark")
                currentThemeName = "DarkWindows11";
            else if (currentThemeName == "Windows10Light")
                currentThemeName = "LightWindows11";
            
            Theme theme = new(currentThemeName);

            if (!IsValidTheme(theme))
                theme = defaultTheme;

            SetTheme(theme);
            if (Enum.TryParse<SystemThemeOptions>(theme.Name, out var t))
                AvaloniaThemeStyle.Theme = t;
            AvaloniaThemeStyle.AccentHue = new HslDiff(settings.Hue+AvaloniaThemeStyle.BaseHue, settings.Saturation, settings.Lightness);
            
            SystemTheme.CustomScalingValue = settings.UseCustomScaling ? Math.Clamp(settings.CustomScaling, 0.5, 4) : null;
        }

        private List<Theme> themes { get; } = Enum.GetNames<SystemThemeOptions>().Select(name => new Theme(name)).ToList();

        public Theme CurrentTheme { get; private set; }

        public IEnumerable<Theme> Themes => themes;

        public void UpdateCustomScaling(double? value)
        {
            SystemTheme.CustomScalingValue = value;
        }
        
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

        public string Part => $"Theme: {CurrentTheme.Name}";
    }
}