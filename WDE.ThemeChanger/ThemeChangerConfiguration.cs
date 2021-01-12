using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.ThemeChanger.Providers;
using WDE.ThemeChanger.ViewModels;
using WDE.ThemeChanger.Views;

namespace WDE.ThemeChanger
{
    [AutoRegister]
    [SingleInstance]
    public class ThemeChangerConfiguration : IConfigurable
    {
        private readonly IThemeManager themeManager;
        private readonly IThemeSettingsProvider themeSettings;

        public ThemeChangerConfiguration(IThemeSettingsProvider settings, IThemeManager themeManager)
        {
            themeSettings = settings;
            this.themeManager = themeManager;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            ThemeConfigView view = new ThemeConfigView();
            ThemeConfigViewModel viewModel = new ThemeConfigViewModel(themeSettings, themeManager);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "Themes";
        }
    }
}