using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaStyles;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Utils;
using Classic.Avalonia.Theme;
using Prism.Commands;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using SixLabors.ImageSharp.ColorSpaces;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers;
using WoWDatabaseEditorCore.Avalonia.Views;
using HslColor = AvaloniaStyles.Utils.HslColor;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.ViewModels
{
    [AutoRegister]
    public partial class ThemeConfigViewModel : ObservableBase, IFirstTimeWizardConfigurable
    {
        private Theme name;
        private List<Theme> themes;

        private ThemeSettings currentSettings;

        public bool IsAprilsFool { get; }

        public ThemeConfigViewModel(IThemeSettingsProvider settings, IThemeManager themeManager, IMainWindowHolder mainWindowHolder)
        {
            IsAprilsFool = DateTime.Today.Month == 4 && DateTime.Today.Day == 1;
            currentSettings = settings.GetSettings();
            name = CurrentThemeName = themeManager.CurrentTheme;
            themes = themeManager.Themes.ToList();
            useCustomScaling = currentSettings.UseCustomScaling;
            scalingValue = Math.Clamp(currentSettings.CustomScaling, 0.5, 4);
            RecommendedScalingPercentage =
                (int)(((mainWindowHolder.RootWindow?.Screens?.Primary ?? mainWindowHolder.RootWindow?.Screens?.All?.FirstOrDefault())?.Scaling ?? 1) * 100);
            AllowCustomScaling = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            ClassicThemeVariant = ClassicTheme.AllVariants.FirstOrDefault(x => x.Key?.ToString() == currentSettings.ThemeVariant);
            
            Save = new DelegateCommand(() =>
            {
                themeManager.SetTheme(ThemeName);
                themeManager.UpdateCustomScaling(useCustomScaling ? ScalingValue : null);
                settings.UpdateSettings(ThemeName,
                    UseCustomScaling ? Math.Clamp(ScalingValue, 0.5, 4) : null,
                    color.H - AvaloniaThemeStyle.BaseHue,
                    color.S, lightness,
                    DateTime.Today >= new DateTime(2025, 04, 01) ? DateTime.Today.Year : null,
                    ClassicThemeVariant?.Key?.ToString());
                currentSettings = settings.GetSettings();
                CurrentThemeName = ThemeName;
                RaisePropertyChanged(nameof(IsModified));
            });

            lightness = currentSettings.Lightness;
            color = new HslColor(currentSettings.Hue+AvaloniaThemeStyle.BaseHue, currentSettings.Saturation, currentSettings.Lightness);

            this.ToObservable(() => Color)
                .Skip(1)
                .SubscribeAction(x =>
                {
                    AvaloniaThemeStyle.AccentHue = new HslDiff(color.H, color.S, lightness);
                });
            
            this.ToObservable(() => Lightness)
                .Skip(1)
                .SubscribeAction(x =>
                {
                    AvaloniaThemeStyle.AccentHue = new HslDiff(color.H, color.S, lightness);
                });
        }

        public Theme CurrentThemeName { get; private set; }

        public Theme ThemeName
        {
            get => name;
            set
            {
                SetProperty(ref name, value);
                RaisePropertyChanged(nameof(IsModified));
            }
        }

        public bool AllowCustomScaling { get; }

        [AlsoNotify(nameof(IsModified))] [Notify] private HslColor color;
        [AlsoNotify(nameof(IsModified))] [Notify] private double lightness = 0.5;

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
                SetProperty(ref useCustomScaling, value);
                RaisePropertyChanged(nameof(IsModified));
            }
        }

        public double ScalingValue
        {
            get => scalingValue;
            set
            {
                SetProperty(ref scalingValue, value);
                RaisePropertyChanged(nameof(IsModified));
                RaisePropertyChanged(nameof(ScalingValuePercentage));
            }
        }

        [Notify] private ThemeVariant? classicThemeVariant;

        public ICommand Save { get; }
        public ImageUri Icon { get; } = new ImageUri("Icons/document_brush_big.png");
        public string Name => "Appearance";
        public string ShortDescription => "Wow Database Editor is supplied with few looks, check them out!";
        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;

        private double scalingValue;
        private bool useCustomScaling;

        public void OnClassicThemeVariantChanged()
        {
            if (classicThemeVariant != null && SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x)
            {
                Application.Current!.RequestedThemeVariant = classicThemeVariant;
            }
            RaisePropertyChanged(nameof(IsModified));
        }

        public bool IsModified =>
            currentSettings.UseCustomScaling != useCustomScaling ||
            Math.Abs(currentSettings.CustomScaling - scalingValue) > 0.001f ||
            CurrentThemeName.Name != ThemeName.Name ||
            Math.Abs(currentSettings.Hue - (color.H - AvaloniaThemeStyle.BaseHue)) > 0.0001f ||
            Math.Abs(currentSettings.Saturation - color.S) > 0.0001f ||
            Math.Abs(currentSettings.Lightness - lightness) > 0.0001f ||
            currentSettings.ThemeVariant != classicThemeVariant?.Key?.ToString();
    }
}