using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles;
using Classic.Avalonia.Theme;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = this;
        if (SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x)
        {
            PART_Classic.IsVisible = true;
            PART_Modern.IsVisible = false;
            this.Width = 400;
            this.Height = 247;
            this.SystemDecorations = SystemDecorations.None;
        }
        else
        {
            PART_Classic.IsVisible = false;
            PART_Modern.IsVisible = true;
            this.Width = 512;
            this.Height = 512;
            this.SystemDecorations = SystemDecorations.BorderOnly;
        }
    }

    protected override void ExtendClientAreaToDecorationsChanged(bool isExtended)
    {
        base.ExtendClientAreaToDecorationsChanged(isExtended);

        // Fix for Windows: disable rounded corners
        if (SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x)
        {
            ClassicWindow.DisableRoundedCorners(this);
        }
    }

}
