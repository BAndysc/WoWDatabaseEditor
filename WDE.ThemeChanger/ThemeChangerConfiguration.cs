using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Module.Attributes;
using System.Windows.Controls;
using WDE.ThemeChanger.Views;
using WDE.ThemeChanger.ViewModels;
using WDE.ThemeChanger.Providers;
using WDE.Common.Managers;

namespace WDE.ThemeChanger
{
    [AutoRegister, SingleInstance]
    public class ThemeChangerConfiguration : IConfigurable
    {
        private readonly IThemeSettingsProvider themeSettings;
        private readonly IThemeManager themeManager;

        public ThemeChangerConfiguration(IThemeSettingsProvider settings, IThemeManager themeManager)
        {
            themeSettings = settings;
            this.themeManager = themeManager;
        }

        public KeyValuePair<ContentControl, Action> GetConfigurationView()
        {
            var view = new ThemeConfigView();
            var viewModel = new ThemeConfigViewModel(themeSettings, themeManager);
            view.DataContext = viewModel;
            return new KeyValuePair<ContentControl, Action>(view, viewModel.SaveAction);
        }

        public string GetName()
        {
            return "Themes";
        }
    }
}
