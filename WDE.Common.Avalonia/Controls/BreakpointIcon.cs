using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WDE.Common.Debugging;
using WDE.Common.Services;

namespace WDE.Common.Avalonia.Controls;

public class BreakpointIcon : Control
{
    public static readonly StyledProperty<bool> IsConnectedProperty =
        AvaloniaProperty.Register<BreakpointIcon, bool>(nameof(IsConnected), true);

    public static readonly StyledProperty<bool> IsDisabledProperty =
        AvaloniaProperty.Register<BreakpointIcon, bool>(nameof(IsDisabled), false);

    public static readonly StyledProperty<bool> IsDeactivatedProperty =
        AvaloniaProperty.Register<BreakpointIcon, bool>(nameof(IsDeactivated), false);

    public static readonly StyledProperty<bool> IsBreakpointHitProperty =
        AvaloniaProperty.Register<BreakpointIcon, bool>(nameof(IsBreakpointHit), false);

    public static readonly StyledProperty<BreakpointState> StateProperty =
        AvaloniaProperty.Register<BreakpointIcon, BreakpointState>(nameof(State));

    public static readonly StyledProperty<bool> SuspendsExecutionProperty =
        AvaloniaProperty.Register<BreakpointIcon, bool>(nameof(SuspendsExecution), false);

    public bool IsConnected
    {
        get => GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    public bool IsDisabled
    {
        get => GetValue(IsDisabledProperty);
        set => SetValue(IsDisabledProperty, value);
    }

    public bool IsDeactivated
    {
        get => GetValue(IsDeactivatedProperty);
        set => SetValue(IsDeactivatedProperty, value);
    }

    public BreakpointState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool SuspendsExecution
    {
        get => GetValue(SuspendsExecutionProperty);
        set => SetValue(SuspendsExecutionProperty, value);
    }

    public bool IsBreakpointHit
    {
        get => GetValue(IsBreakpointHitProperty);
        set => SetValue(IsBreakpointHitProperty, value);
    }

    static BreakpointIcon()
    {
        AffectsRender<BreakpointIcon>(IsConnectedProperty);
        AffectsRender<BreakpointIcon>(IsDisabledProperty);
        AffectsRender<BreakpointIcon>(IsDeactivatedProperty);
        AffectsRender<BreakpointIcon>(StateProperty);
        AffectsRender<BreakpointIcon>(SuspendsExecutionProperty);
        AffectsRender<BreakpointIcon>(IsBreakpointHitProperty);
    }

    public static string GetToolTip(bool isConnected, bool isDeactivated, bool isDisabled, BreakpointState state)
    {
        if (!isConnected)
            return "The editor is not connected to the server. The breakpoint will be synced once you start the server.";

        if (isDeactivated)
            return "The breakpoint is deactivated, because this breakpoint is linked to a document (i.e. a smart script) that is currently closed.";

        if (isDisabled)
            return "The breakpoint is disabled by you. Right click on the breakpoint to enable/disable it.";

        switch (state)
        {
            case BreakpointState.Pending:
                return "The breakpoint is being sent to the server";
            case BreakpointState.PendingRemoval:
                return "The breakpoint is being removed from the server";
            case BreakpointState.Synced:
                return "The breakpoint is enabled and synchronized";
            case BreakpointState.WaitingForSync:
                return "The breakpoint is out of sync and will be synced after the document is saved";
            case BreakpointState.SynchronizationError:
                return "The breakpoint couldn't be synced due to an error";
        }

        return state.ToString();
    }

    private static void DrawArrowIcon(DrawingContext context, Rect rect)
    {
        Point PointAt(double x, double y) => new Point(rect.X + rect.Width * x, rect.Y + rect.Height * y);

        var brush = new SolidColorBrush(new Color(255, 245, 210, 54));

        var poly = new PolylineGeometry()
        {
            Points = new Points()
            {
                PointAt(0.57, 0.64),
                PointAt(0.07, 0.64),
                PointAt(0.07, 0.36),
                PointAt(0.57, 0.36),
                PointAt(0.57, 0.11),
                PointAt(0.98, 0.5),
                PointAt(0.57, 0.89),
            },
            IsFilled = true
        };
        context.DrawGeometry(brush, new Pen(Brushes.White, 1.5f), poly);
    }

    public static void DrawIcon(DrawingContext context, bool isConnected, bool isDeactivated, bool isDisabled,
        BreakpointState state, bool suspendsExecution, bool isBreakpointHit, Rect rect)
    {
        IBrush? brush = null;
        IPen? pen = null;
        if (!isConnected || isDeactivated)
        {
            brush = Brushes.Transparent;
            pen = new Pen(new SolidColorBrush(new Color(255, 184, 184, 184)), 2);
        }
        else if (isDisabled)
        {
            brush = new SolidColorBrush(new Color(255, 184, 184, 184));
        }
        else if (state == BreakpointState.Synced)
        {
            var noStop = !suspendsExecution;

            brush = noStop ? new SolidColorBrush(new Color(255, 243,175,16)) :
                new SolidColorBrush(new Color(255, 242, 78, 91));
        }
        else if (state == BreakpointState.Pending || state == BreakpointState.PendingRemoval)
        {
            brush = new SolidColorBrush(new Color(255, 184, 184, 184));
        }
        else if (state == BreakpointState.WaitingForSync)
        {
            brush = Brushes.Transparent;
            pen = new Pen(new SolidColorBrush(new Color(255, 242, 78, 91)), 2);
        }
        else if (state == BreakpointState.SynchronizationError)
        {
            brush = Brushes.Transparent;
            pen = new Pen(new SolidColorBrush(new Color(255, 242, 78, 91)), 2);
        }

        context.DrawEllipse(brush, pen, rect.Center, rect.Width / 2, rect.Height / 2);

        if (state == BreakpointState.SynchronizationError)
        {
            var margin = 4;
            var exclaimationP1 = new Point(rect.Center.X, rect.Top + margin * 0.75f);
            var exclaimationP2 = new Point(rect.Center.X, rect.Bottom - margin * 1.75f);
            // poor's man exclamation mark (buy hey, it works and is fast)
            if (pen != null)
            {
                context.DrawLine(pen, exclaimationP1, exclaimationP2);
                context.DrawEllipse(pen.Brush, null, new Point(rect.Center.X, rect.Bottom - margin), 1.5f, 1.5f);
            }
        }

        if (isBreakpointHit)
        {
            DrawArrowIcon(context, rect.Deflate(rect.Width * 0.1));
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        var size = Math.Min(bounds.Width, bounds.Height);

        DrawIcon(context, IsConnected, IsDeactivated, IsDisabled, State, SuspendsExecution, IsBreakpointHit, new Rect(bounds.Width / 2 - size / 2, bounds.Height / 2 - size / 2, size, size));
    }
}