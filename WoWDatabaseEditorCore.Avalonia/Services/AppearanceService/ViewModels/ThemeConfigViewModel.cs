using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.ViewModels
{
    [AutoRegister]
    public class ThemeConfigViewModel : BindableBase, IConfigurable
    {
        private Theme name;
        private List<Theme> themes;

        public ThemeConfigViewModel(IThemeSettingsProvider settings, IThemeManager themeManager)
        {
            name = CurrentThemeName = themeManager.CurrentTheme;
            themes = themeManager.Themes.ToList();

            Save = new DelegateCommand(() =>
            {
                themeManager.SetTheme(ThemeName);
                settings.UpdateSettings(ThemeName);
                IsModified = false;
            });
        }
        
        public Theme CurrentThemeName { get; }

        public Theme ThemeName
        {
            get => name;
            set
            {
                IsModified = true;
                SetProperty(ref name, value);
            }
        }

        public List<Theme> Themes
        {
            get => themes;
            set => SetProperty(ref themes, value);
        }

        public ICommand Save { get; }
        public string Name => "Appearance";
        public string ShortDescription => "Wow Database Editor is supplied with few looks, check them out!";
        public bool IsRestartRequired => true;

        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
    }
}