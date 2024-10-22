using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Classic.Avalonia.Theme;

namespace WoWDatabaseEditorCore.Avalonia.Clippy;

public partial class ClippyWindow : ClassicWindow
{
    private Popup? popup;
    private bool once = false;
    private ClippyViewModel? viewModel;

    public ClippyWindow()
    {
        InitializeComponent();
        this.AttachDevTools();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
    }

    private void OpenPopup()
    {
        popup = new Popup()
        {
            IsLightDismissEnabled = false
        };
        popup.Topmost = false;
        popup.Width = 254;
        popup.VerticalOffset = -20;
        popup.Child = (this.Resources["PopupContentTemplate"] as Template)!.Build();
        popup.PlacementTarget = this;
        popup.Placement = PlacementMode.Top;
        LogicalChildren.Add(popup);
        popup.Open();
    }

    private void ClosePopup()
    {
        popup?.Close();
        popup = null;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ClippyViewModel viewModel)
        {
            this.viewModel = viewModel;
            if (once)
                return;
            once = true;
            viewModel.Activated();
            viewModel.PropertyChanged += OnPopup;
        }
    }

    private void OnPopup(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClippyViewModel.Question))
        {
            if (viewModel?.Question != null)
            {
                ClosePopup();
                OpenPopup();
            }
            else
            {
                ClosePopup();
            }
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (viewModel != null)
        {
            viewModel.PropertyChanged -= OnPopup;
            viewModel.CloseQuestion();
        }
    }
}