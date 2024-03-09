using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Prism.Commands;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.ViewModels;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.Views;

public partial class BreakpointHitPopupView : UserControl
{
    private Popup? popup;
    private AdornerLayer? attachedAdornerLayer;
    private Control? adornedElement;

    private TaskCompletionSource openedTask = new();

    public BreakpointHitPopupView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async Task OpenAsPopup(Control owner, double offsetX, double offsetY)
    {
        if (popup == null)
        {
            popup = new Popup()
            {
                Child = this,
                Placement = PlacementMode.AnchorAndGravity,
                PlacementGravity = PopupGravity.BottomRight,
                PlacementAnchor = PopupAnchor.TopLeft,
                PlacementTarget = owner,
                HorizontalOffset = offsetX,
                VerticalOffset = offsetY
            };
        }

        openedTask = new TaskCompletionSource();
        popup.Closed += OnPopupClosed;
        if (!ReferenceEquals(popup.Parent, owner))
        {
            ((ISetLogicalParent)popup).SetParent(null);
            ((ISetLogicalParent)popup).SetParent(owner);
        }
        popup.Open();
        await openedTask.Task;
    }

    public async Task OpenAsAdorner(Control owner, double offsetX, double offsetY)
    {
        attachedAdornerLayer = AdornerLayer.GetAdornerLayer(owner);
        if (attachedAdornerLayer == null)
            return;

        openedTask = new TaskCompletionSource();
        adornedElement = owner;

        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;

        attachedAdornerLayer.Children.Add(this);
        AdornerLayer.SetIsClipEnabled(this, false);
        AdornerLayer.SetAdornedElement(this, adornedElement);
        Margin = new Thickness(offsetX, offsetY, 0, 0);

        await openedTask.Task;
    }

    public bool Close()
    {
        if (popup != null)
        {
            popup.Close();
            return true;
        }
        else if (attachedAdornerLayer != null &&
                 adornedElement != null)
        {
            attachedAdornerLayer.Children.Remove(this);
            attachedAdornerLayer = null;
            adornedElement = null;
            openedTask.TrySetResult();
            return true;
        }

        return false;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        openedTask.TrySetResult();
        popup!.Closed -= OnPopupClosed;
    }

    private void ClosePressed(object? sender, RoutedEventArgs e)
    {
        if (!Close())
        {
            if (DataContext is BreakpointHitPopupViewModel vm)
            {
                vm.CloseCommand.Execute(null);
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // workaround for popups that shall be taller than parent's height
        // when the popup is attached as adorner (that would be clipped normally, but setting minheight fixes it)
        var size = base.MeasureOverride(new Size(double.MaxValue, double.MaxValue));
        MinHeight = size.Height;
        MinWidth = size.Width;
        return size;
    }
}