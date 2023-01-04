using System;
using Avalonia.Markup.Xaml;

namespace AvaloniaStyles.Styles;

public class Windows10Light : Avalonia.Styling.Styles
{
    public Windows10Light(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}