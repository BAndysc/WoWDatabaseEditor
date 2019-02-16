using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.ThemeChanger.Providers;
using Prism.Mvvm;
using System.Windows;
using WDE.Common.Managers;

namespace WDE.ThemeChanger.ViewModels
{
    public class ThemeConfigViewModel : BindableBase
    {
        private readonly IThemeSettingsProvider settings;
        private readonly IThemeManager themeManager;
        public Action SaveAction { get; set; }

        private Theme _name;
        private List<Theme> _themes;

        public Theme Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); themeManager.SetTheme(value); }
        }

        public List<Theme> Themes
        {
            get { return _themes; }
            set { SetProperty(ref _themes, value); }
        }

        public ThemeConfigViewModel(IThemeSettingsProvider s, IThemeManager themeManager)
        {
            this.themeManager = themeManager;
            SaveAction = Save;

            _name = themeManager.CurrentTheme;
            _themes = themeManager.Themes.ToList();

            settings = s;
        }

        private void Save()
        {
            settings.UpdateSettings(Name);
        }
    }
}
