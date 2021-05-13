using System;
using Avalonia.Markup.Xaml.Styling;
using JetBrains.Annotations;

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
                Source = SystemTheme.EffectiveThemeIsDark ? DarkSource : LightSource;
            }
        }
        
        private Uri? darkSource;
        public Uri? DarkSource
        {
            get => darkSource;
            set
            {
                darkSource = value;
                Source = SystemTheme.EffectiveThemeIsDark ? DarkSource : LightSource;
            }
        }
        
        public StyleIncludeColorAware([NotNull] Uri baseUri) : base(baseUri)
        {
        }

        public StyleIncludeColorAware([NotNull] IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}