using System;
using Avalonia;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Clippy;

[AutoRegister]
[SingleInstance]
public class ClippyService : IClippy
{
    private readonly IMainWindowHolder mainWindowHolder;
    private readonly ClippyViewModel clippyViewModel;
    private ClippyWindow? clippy;

    public ClippyService(IMainWindowHolder mainWindowHolder, ClippyViewModel clippyViewModel)
    {
        this.mainWindowHolder = mainWindowHolder;
        this.clippyViewModel = clippyViewModel;
    }

    private ClippyWindow GetClippy()
    {
        if (clippy == null)
        {
            clippy = new ClippyWindow()
            {
                DataContext = clippyViewModel
            };
            clippy.Closed += OnClippyClosed;
        }

        return clippy;
    }

    public void Open()
    {
        if (mainWindowHolder.RootWindow is { } rootWindow &&
            GetClippy() is { } clippy)
        {
            clippy.Show(rootWindow);
            clippy.Position = new PixelPoint((int)(rootWindow.Position.X + rootWindow.Width - clippy.Width - 124),
                (int)(rootWindow.Position.Y + rootWindow.Height - clippy.Height - 92));
        }
    }

    private void OnClippyClosed(object? sender, EventArgs e)
    {
        if (clippy != null)
        {
            clippy.Closed -= OnClippyClosed;
        }
        clippy = null;
    }
}