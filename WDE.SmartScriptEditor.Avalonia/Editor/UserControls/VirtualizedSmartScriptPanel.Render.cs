using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Debugging;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Avalonia.Extensions;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public class VirtualizedSmartScriptPanelRenderOverlay : Control
{
    private readonly VirtualizedSmartScriptPanel parent;

    public VirtualizedSmartScriptPanelRenderOverlay(VirtualizedSmartScriptPanel parent)
    {
        this.parent = parent;
        IsHitTestVisible = false;
    }
    
    public override void Render(DrawingContext context)
    {
        parent.RenderOverlay(context);
    }
}


public class FormattedTextNumberCache
{
    private FormattedText[] cache = new FormattedText[0];

    public FormattedTextNumberCache()
    {
                
    }

    public FormattedText Get(int index)
    {
        if (cache.Length <= index)
            EnsureCache(index + 1);
        return cache[index];
    }

    private void EnsureCache(int size)
    {
        int old = cache.Length;
        size = Math.Max(size, cache.Length * 2 + 1);
        Array.Resize(ref cache, size);
        for (int i = old; i < size; ++i)
        {
            cache[i] = new FormattedText($"{i}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.DarkGray)
            {
            };
        }
    }
}

public partial class VirtualizedSmartScriptPanel
{
    private static FormattedText? vvvvText;
    private static FormattedTextNumberCache NumberCache = new();
    public void RenderOverlay(DrawingContext context)
    {
        base.Render(context);
        if (script == null)
            return;

        SizingContext sizing = new SizingContext(Bounds.Width, Padding, EventPaddingLeft, VisibleRect);
        
        if (AnythingSelected())
        {
            if (draggingActions)
            {
                double x = sizing.eventRect.Right;
                double y = overIndexAction.y - overIndexAction.height / 2 - 1;
                context.DrawArrow(new Pen(Brushes.Gray, 3), new Point(x, y), new Point(x + 200, y), 10);
            }
            else if (draggingConditions)
            {
                double x = sizing.conditionRect.X;
                double y = overIndexCondition.y - overIndexCondition.height / 2 - 1;
                context.DrawArrow(new Pen(Brushes.Gray, 3), new Point(x, y), new Point(sizing.conditionRect.Right, y), 10);         
            }
            else if (draggingEvents)
            {
                double x = OverIndexEvent.inGroup ?  sizing.groupedEventRect.X : sizing.eventRect.X;
                double right = OverIndexEvent.inGroup ? sizing.groupedEventRect.Right : sizing.eventRect.Right;
                double y = OverIndexEvent.y;
                context.DrawArrow(new Pen(Brushes.Gray, 3), new Point(x, y), new Point(right, y), 10);
            }
            else if (draggingGroups)
            {
                double x = sizing.groupRect.X;
                double right = sizing.groupRect.Right;
                double y = overIndexGroup.y;
                context.DrawArrow(new Pen(Brushes.Gray, 3), new Point(x, y), new Point(right, y), 10);
            }
        }
        
        if (vvvvText == null)
        {
            vvvvText = new FormattedText("vvvv", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 7, null);
        }
        
        var visibleRect = VisibleRect;
        var breakpoints = Breakpoints;

        bool inGroup = false;
        bool groupIsExpanded = false;
        foreach (var e in script.Events)
        {
            if (e.IsBeginGroup)
            {
                inGroup = true;
                groupIsExpanded = new SmartGroup(e).IsExpanded;
            }
            else if (e.IsEndGroup)
            {
                inGroup = false;
            }
            else
            {
                if (inGroup && !groupIsExpanded)
                    continue;
             
                if (e.Actions.Count == 0)
                {
                    if (!visibleRect.Intersects(e.EventPosition.ToRect()))
                        continue;
                
                    double yPos = e.Position.Y;

                    DrawProblems(context, e.VirtualLineId, new Point(0, yPos));
                }
                else
                {
                    foreach (var a in e.Actions)
                    {
                        if (a.Id == SmartConstants.ActionComment && HideComments)
                            continue;

                        if (!visibleRect.Intersects(a.Position.ToRect()))
                            continue;
                    
                        double yPos = a.Position.Y;

                        float x = a.IsInInlineActionList ? 10 : 0;
                        int? eventId = a.DestinationEventId;

                        if (a.Id == SmartConstants.ActionComment && a == e.Actions[0])
                            eventId = e.DestinationEventId;

                        DebugPointId? breakpoint = null;
                        bool isAnyHit = false;
                        bool drawBreakpoint = false;
                        if (breakpoints != null)
                        {
                            if (breakpoints.GetBreakpoint(a) is { } actionBreakpoint)
                            {
                                breakpoint ??= actionBreakpoint;
                                isAnyHit |= breakpoints.IsHit(actionBreakpoint);
                            }

                            if (breakpoints.GetBreakpoint(a.Source) is { } sourceBreakpoint)
                            {
                                breakpoint ??= sourceBreakpoint;
                                isAnyHit |= breakpoints.IsHit(sourceBreakpoint);
                            }

                            if (breakpoints.GetBreakpoint(a.Target) is { } targetBreakpoint)
                            {
                                breakpoint ??= targetBreakpoint;
                                isAnyHit |= breakpoints.IsHit(targetBreakpoint);
                            }

                            if (a == e.Actions[0] && breakpoints.GetBreakpoint(e) is { } eventBreakpoint)
                            {
                                breakpoint ??= eventBreakpoint;
                                isAnyHit |= breakpoints.IsHit(eventBreakpoint);
                            }

                            var breakPointRect = GetBreakpointRect(a);
                            if (breakpoint.HasValue)
                            {
                                BreakpointIcon.DrawIcon(context,
                                    breakpoints.IsConnected,
                                    breakpoints.IsDeactivated(breakpoint.Value),
                                    breakpoints.IsDisabled(breakpoint.Value),
                                    breakpoints.GetState(breakpoint.Value),
                                    breakpoints.IsSuspendExecution(breakpoint.Value),
                                    isAnyHit,
                                    breakPointRect);
                            }
                            else if (breakPointRect.Contains(new Point(mouseX, mouseY)))
                            {
                                context.DrawEllipse(new SolidColorBrush(new Color(120, 242, 78, 91)), null, breakPointRect.Center, BreakpointRadius, BreakpointRadius);
                            }
                            else
                            {
                                drawBreakpoint = true;
                            }
                        }
                        if ((breakpoints == null || drawBreakpoint) && eventId.HasValue)
                        {
                            var ft = NumberCache.Get(eventId.Value);
                            context.DrawText(ft, new Point(x + EventPaddingLeft - ft.Width, yPos + 6));
                        }
                        DrawProblems(context, a.VirtualLineId, new Point(x, yPos));
                    }
                }
            }
        }
    }

    private Rect GetBreakpointRect(VisualSmartBaseElement e)
    {
        return new Rect(PaddingLeft, e.Position.Y + 2, BreakpointRadius * 2, BreakpointRadius * 2);
    }

    private void DrawProblems(DrawingContext dc, int index, Point pos)
    {
        if (Problems != null && Problems.TryGetValue(index, out var severity))
        { 
            vvvvText!.SetForegroundBrush(severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical ? Brushes.Red : Brushes.Orange);
            dc.DrawText(vvvvText, new Point(PaddingLeft + pos.X, pos.Y + 5 + 10));   
        }
    }

}
