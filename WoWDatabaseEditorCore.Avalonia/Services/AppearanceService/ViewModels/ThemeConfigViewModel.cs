using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaStyles;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Utils;
using Prism.Commands;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using SixLabors.ImageSharp.ColorSpaces;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.ViewModels
{
    [AutoRegister]
    public partial class ThemeConfigViewModel : ObservableBase, IFirstTimeWizardConfigurable
    {
        private Theme name;
        private List<Theme> themes;

        public ThemeConfigViewModel(IThemeSettingsProvider settings, IThemeManager themeManager, IMainWindowHolder mainWindowHolder)
        {
            var currentSettings = settings.GetSettings();
            name = CurrentThemeName = themeManager.CurrentTheme;
            themes = themeManager.Themes.ToList();
            useCustomScaling = currentSettings.UseCustomScaling;
            scalingValue = Math.Clamp(currentSettings.CustomScaling, 0.5, 4);
            RecommendedScalingPercentage =
                (int)(((mainWindowHolder.Window?.Screens?.Primary ?? mainWindowHolder.Window?.Screens?.All?.FirstOrDefault())?.PixelDensity ?? 1) * 100);
            AllowCustomScaling = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
            Save = new DelegateCommand(() =>
            {
                themeManager.SetTheme(ThemeName);
                themeManager.UpdateCustomScaling(useCustomScaling ? ScalingValue : null);
                settings.UpdateSettings(ThemeName, UseCustomScaling ? Math.Clamp(ScalingValue, 0.5, 4) : null, color.H - AvaloniaThemeStyle.BaseHue, color.S, lightness);
                IsModified = false;
            });

            lightness = currentSettings.Lightness;
            color = new HslColor(currentSettings.Hue+AvaloniaThemeStyle.BaseHue, currentSettings.Saturation, currentSettings.Lightness);

            this.ToObservable(() => Color)
                .Skip(1)
                .SubscribeAction(x =>
                {
                    AvaloniaThemeStyle.AccentHue = new HslDiff(color.H, color.S, lightness);
                    IsModified = true;
                });
            this.ToObservable(() => Lightness)
                .Skip(1)
                .SubscribeAction(x =>
                {
                    AvaloniaThemeStyle.AccentHue = new HslDiff(color.H, color.S, lightness);
                    IsModified = true;
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

        public bool AllowCustomScaling { get; }

        [Notify] private HslColor color;
        [Notify] private double lightness = 0.5;

        public List<Theme> Themes
        {
            get => themes;
            set => SetProperty(ref themes, value);
        }

        public int RecommendedScalingPercentage { get; }
        
        public int ScalingValuePercentage => (int)(ScalingValue * 100);

        public bool UseCustomScaling
        {
            get => useCustomScaling;
            set
            {
                IsModified = true;
                SetProperty(ref useCustomScaling, value);
            }
        }

        public double ScalingValue
        {
            get => scalingValue;
            set
            {
                IsModified = true;
                SetProperty(ref scalingValue, value); 
                RaisePropertyChanged(nameof(ScalingValuePercentage));
            }
        }

        public ICommand Save { get; }
        public string Name => "Appearance";
        public string ShortDescription => "Wow Database Editor is supplied with few looks, check them out!";
        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;

        private bool isModified;
        private double scalingValue;
        private bool useCustomScaling;

        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
    }
}