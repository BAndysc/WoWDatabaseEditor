using System;
using Avalonia.Markup.Xaml.Styling;

namespace AvaloniaStyles.Styles
{
    public class StyleIncludeColorAware : StyleInclude
    {
        private Uri? lightSource;
        public Uri? LightSource
        {
            get => lightSource;
            set
            {
                lightSource = value;
                UpdateSource();
            }
        }

        private Uri? darkSource;
        public Uri? DarkSource
        {
            get => darkSource;
            set
            {
                darkSource = value;
                UpdateSource();
            }
        }

        private Uri? win9x;
        public Uri? Win9x
        {
            get => win9x;
            set
            {
                win9x = value;
                UpdateSource();
            }
        }

        private void UpdateSource()
        {
            Source = SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x && Win9x != null ? Win9x : (SystemTheme.EffectiveThemeIsDark ? DarkSource : LightSource);
        }

        public StyleIncludeColorAware(Uri baseUri) : base(baseUri)
        {
        }

        public StyleIncludeColorAware(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}