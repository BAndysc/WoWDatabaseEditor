using System;
using System.Runtime.InteropServices;
using Avalonia.Markup.Xaml.Styling;

namespace AvaloniaStyles
{
    public enum SystemThemeOptions
    {
        Auto,
        MacOsCatalinaLight,
        MacOsCatalinaDark,
        MacOsBigSurLight,
        MacOsBigSurDark,
        Windows10Dark,
        Windows10Light
    }
    
    public class SystemTheme : StyleInclude
    {
        internal static SystemThemeOptions EffectiveTheme { get; private set; }
        internal static bool EffectiveThemeIsDark { get; private set; }
        
        public SystemTheme(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public bool ThemeUsesDock =>
            mode == SystemThemeOptions.Windows10Dark || mode == SystemThemeOptions.Windows10Light;

        protected static SystemThemeOptions ResolveTheme(SystemThemeOptions theme)
        {
            if (theme != SystemThemeOptions.Auto) 
                return theme;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var version = Environment.OSVersion;
                if (version.Version.Major >= 11)
                    return SystemThemeOptions.MacOsBigSurLight;
                else
                    return SystemThemeOptions.MacOsCatalinaLight;
            }
            
            return SystemThemeOptions.Windows10Dark;
        }
        
        private SystemThemeOptions mode;
        public SystemThemeOptions Mode
        {
            get => mode;
            set
            {
                mode = value;
                EffectiveTheme = ResolveTheme(Mode);
                EffectiveThemeIsDark = EffectiveTheme == SystemThemeOptions.Windows10Dark ||
                                       EffectiveTheme == SystemThemeOptions.MacOsCatalinaDark ||
                                       EffectiveTheme == SystemThemeOptions.MacOsBigSurDark;

                switch (EffectiveTheme)
                {
                    case SystemThemeOptions.MacOsCatalinaLight:
                        Source = new Uri("avares://AvaloniaStyles/Styles/MacOsCatalinaLight.xaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.MacOsCatalinaDark:
                        Source = new Uri("avares://AvaloniaStyles/Styles/MacOsCatalinaDark.xaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.MacOsBigSurLight:
                        Source = new Uri("avares://AvaloniaStyles/Styles/MacOsBigSurLight.xaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.MacOsBigSurDark:
                        Source = new Uri("avares://AvaloniaStyles/Styles/MacOsBigSurDark.xaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.Windows10Dark:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows10Dark.xaml", UriKind.Absolute);
                        break;
                    case SystemThemeOptions.Windows10Light:
                        Source = new Uri("avares://AvaloniaStyles/Styles/Windows10Light.xaml", UriKind.Absolute);
                        break;
                }
            }
        }
    }
}