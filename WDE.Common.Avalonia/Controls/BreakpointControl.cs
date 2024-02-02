using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Prism.Events;
using WDE.Common.Avalonia.Debugging;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Debugging;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

/// <summary>
///  more automatic than BreakpointIcon - here you just provide the DebugId and it will automatically connect to the service and get the breakpoint state
/// </summary>
public class BreakpointControl : Control
{
    public static readonly StyledProperty<DebugPointId> DebugPointIdProperty =
        AvaloniaProperty.Register<BreakpointControl, DebugPointId>(nameof(DebugPointId), DebugPointId.Empty);

    public DebugPointId DebugPointId
    {
        get => GetValue(DebugPointIdProperty);
        set => SetValue(DebugPointIdProperty, value);
    }

    static BreakpointControl()
    {
        AffectsRender<BreakpointControl>(DebugPointIdProperty);
        DebugPointIdProperty.Changed.AddClassHandler<BreakpointControl>((icon, args) => icon.UpdateToolTip());
        ContextRequestedEvent.AddClassHandler<BreakpointControl>((icon, e) => icon.OnContextRequested(e));
    }

    private IDebuggerService? boundService;
    private System.IDisposable? breakpointHitSubscription;

    private void Unbind()
    {
        if (boundService != null)
        {
            boundService.DebugPointAdded -= OnDebugPointAdded;
            boundService.DebugPointRemoved -= OnDebugPointRemoved;
            boundService.DebugPointChanged -= OnDebugPointChanged;
            boundService = null;
        }
        breakpointHitSubscription?.Dispose();
        breakpointHitSubscription = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unbind();
        base.OnAttachedToVisualTree(e);

        IDebuggerService? service = ViewBind.ResolveViewModel<IDebuggerService>();
        IEventAggregator? eventAggregator = ViewBind.ResolveViewModel<IEventAggregator>();
        if (service == null || eventAggregator == null)
            return;
        service.DebugPointAdded += OnDebugPointAdded;
        service.DebugPointRemoved += OnDebugPointRemoved;
        service.DebugPointChanged += OnDebugPointChanged;
        boundService = service;
        UpdateToolTip();
        breakpointHitSubscription = eventAggregator.GetEvent<IdeBreakpointRequestPopupEvent>()
            .Subscribe(e =>
            {
                if (e.HitDebugPoint == DebugPointId)
                {
                    e.AttachPopupToObject = this;
                    e.PopupOffsetX = 0;
                    e.PopupOffsetY = Bounds.Height;
                }
            });
    }

    private void OnDebugPointChanged(DebugPointId id)
    {
        if (id == DebugPointId)
        {
            InvalidateVisual();
            UpdateToolTip();
        }
    }

    private void OnDebugPointRemoved(DebugPointId id)
    {
        if (id == DebugPointId)
        {
            InvalidateVisual();
            UpdateToolTip();
        }
    }

    private void OnDebugPointAdded(DebugPointId id)
    {
        if (id == DebugPointId)
        {
            InvalidateVisual();
            UpdateToolTip();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unbind();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnContextRequested(ContextRequestedEventArgs e)
    {
        var id = DebugPointId;
        if (id == DebugPointId.Empty || boundService == null || !boundService.HasDebugPoint(id))
            return;

        ViewBind.ResolveViewModel<IEditDebugPointService>().EditDebugPointInPopup(this, id).ListenErrors();
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var id = DebugPointId;
        if (id == DebugPointId.Empty)
            return;

        if (boundService == null)
            return;

        if (!boundService.HasDebugPoint(id))
            return;

        var bounds = Bounds;
        var size = Math.Min(bounds.Width, bounds.Height);

        var isConnected = boundService.IsConnected;
        var isDisabled = !boundService.GetEnabled(id);
        var isDeactivated = !boundService.GetActivated(id);
        var state = boundService.GetState(id);
        var suspendsExecution = boundService.GetSuspendExecution(id);
        var isHit = boundService.IsBreakpointHit(id);

        BreakpointIcon.DrawIcon(context, isConnected, isDeactivated, isDisabled, state, suspendsExecution, isHit, new Rect(bounds.Width / 2 - size / 2, bounds.Height / 2 - size / 2, size, size));
    }

    private void UpdateToolTip()
    {
        void DisableToolTip()
        {
            ToolTip.SetIsOpen(this, false);
            ToolTip.SetTip(this, null);
        }
        var id = DebugPointId;
        if (id == DebugPointId.Empty)
        {
            DisableToolTip();
            return;
        }

        if (boundService == null)
        {
            DisableToolTip();
            return;
        }

        if (!boundService.HasDebugPoint(id))
        {
            DisableToolTip();
            return;
        }

        var state = boundService.GetState(id);
        var isConnected = boundService.IsConnected;
        var isDisabled = !boundService.GetEnabled(id);
        var isDeactivated = !boundService.GetActivated(id);
        // why not set string directly?
        // for some reason tooltips in popups (DebuggingPopup) are not working properly in Avalonia 10
        // but works when it is TextBlock
        ToolTip.SetTip(this, new TextBlock(){Text = BreakpointIcon.GetToolTip(isConnected, isDeactivated, isDisabled, state)});
    }
}