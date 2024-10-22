using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Metadata;
using Avalonia.Styling;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "AvaloniaStyles.Controls")]

namespace AvaloniaStyles
{
    public enum SystemThemeOptions
    {
        Auto,
        //MacOsCatalinaLight,
        //MacOsCatalinaDark,
        //MacOsBigSurLight,
        //MacOsBigSurDark,
        DarkWindows10,
        LightWindows10,
        DarkWindows11,
        LightWindows11,
        Windows9x
    }
    
    public class SystemTheme : StyleInclude
    {
        public static event Action<double?>? CustomScalingUpdated;

        public static double? CustomScalingValue
        {
            get => customScalingValue;
            set
            {
                customScalingValue = value;
                CustomScalingUpdated?.Invoke(value);
            }
        }

        public static SystemThemeOptions EffectiveTheme { get; private set; }
        public static bool EffectiveThemeIsDark { get; private set; }
        
        public static Avalonia.Media.Color ForegroundColor => EffectiveThemeIsDark ? Avalonia.Media.Colors.White : Avalonia.Media.Colors.Black;
        
        public SystemTheme(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public bool ThemeUsesDock =>
            mode is SystemThemeOptions.DarkWindows10 or SystemThemeOptions.DarkWindows11 or
                SystemThemeOptions.LightWindows10 or SystemThemeOptions.LightWindows11 or SystemThemeOptions.Windows9x;

        protected static SystemThemeOptions ResolveTheme(SystemThemeOptions theme)
        {
            if (theme != SystemThemeOptions.Auto) 
                return theme;

            var os = Environment.OSVersion.Version;
            // 11 style even for win 10, why not :)
            // if (OperatingSystem.IsWindows() && os.Major == 10)
            //     return SystemThemeOptions.LightWindows10;
            return SystemThemeOptions.LightWindows11;
        }
        
        private SystemThemeOptions mode;
        private static double? customScalingValue;

        public SystemThemeOptions Mode
        {
            get => mode;
            set
            {
                mode = value;
                EffectiveTheme = ResolveTheme(Mode);
                EffectiveThemeIsDark = EffectiveTheme is SystemThemeOptions.DarkWindows10 or SystemThemeOptions.DarkWindows11;
                                       // || EffectiveTheme == SystemThemeOptions.MacOsCatalinaDark ||
                                       // EffectiveTheme == SystemThemeOptions.MacOsBigSurDark;

               Application.Current!.RequestedThemeVariant = EffectiveThemeIsDark
                   ? ThemeVariant.Dark
                   : ThemeVariant.Light;
                                       
                switch (EffectiveTheme)
                {
                    // case SystemThemeOptions.MacOsCatalinaLight:
                    //     Source = new Uri("avares://AvaloniaStyles/Styles/MacOsCatalinaLight.xaml", UriKind.Absolute);
                    //     break;
                    // case SystemThemeOptions.MacOsCatalinaDark:
                    //     Source = new Uri("avares://AvaloniaStyles/Styles/MacOsCatalinaDark.xaml", UriKind.Absolute);
                    //     break;
                    // case SystemThemeOptions.MacOsBigSurLight:
                    //     Source = new Uri("avares://AvaloniaStyles/Styles/MacOsBigSurLight.xaml", UriKind.Absolute);
                    //     break;
                    // case SystemThemeOptions.MacOsBigSurDark:
                    //     Source = new Uri("avares://AvaloniaStyles/Styles/MacOsBigSurDark.xaml", UriKind.Absolute);
                    //     break;
                    case SystemThemeOptions.DarkWindows10:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows10Dark.axaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.LightWindows10:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows10Light.axaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.DarkWindows11:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows11Dark.axaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.LightWindows11:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows11Light.axaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.Windows9x:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows9x.axaml", UriKind.Absolute);
                        break;
                }
            }
        }

        public static ThemeVariant EffectiveThemeVariant => EffectiveThemeIsDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}