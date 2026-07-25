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
            // waiting for https://github.com/AvaloniaUI/Avalonia/pull/21615/changes#diff-4f145a8afffe2e7471d689e046b841c087df73ce20ea18e6795e90a15be70a43
            // Win32Properties.SetWindowCornerPreference(this, WindowCornerPreference.DoNotRound);
            this.WindowDecorations = WindowDecorations.None;
        }
        else
        {
            PART_Classic.IsVisible = false;
            PART_Modern.IsVisible = true;
            this.Width = 512;
            this.Height = 512;
            this.WindowDecorations = WindowDecorations.BorderOnly;
        }
    }
}
