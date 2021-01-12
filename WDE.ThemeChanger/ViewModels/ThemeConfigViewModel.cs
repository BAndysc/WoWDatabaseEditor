using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.ThemeChanger.Providers;

namespace WDE.ThemeChanger.ViewModels
{
    public class ThemeConfigViewModel : BindableBase
    {
        private readonly IThemeSettingsProvider settings;
        private readonly IThemeManager themeManager;

        private Theme name;
        private List<Theme> themes;

        public ThemeConfigViewModel(IThemeSettingsProvider s, IThemeManager themeManager)
        {
            this.themeManager = themeManager;
            SaveAction = Save;

            name = themeManager.CurrentTheme;
            themes = themeManager.Themes.ToList();

            settings = s;
        }

        public Action SaveAction { get; set; }

        public Theme Name
        {
            get => name;
            set
            {
                SetProperty(ref name, value);
                themeManager.SetTheme(value);
            }
        }

        public List<Theme> Themes
        {
            get => themes;
            set => SetProperty(ref themes, value);
        }

        private void Save()
        {
            settings.UpdateSettings(Name);
        }
    }
}