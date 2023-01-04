using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WDE.Common.Avalonia.Utils;
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
            cache[i] = new FormattedText($"{i}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, Brushes.DarkGray);
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

        int index = 0;
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
                {
                    index += Math.Max(1, e.Actions.Count);
                    continue;
                }
             
                if (e.Actions.Count == 0)
                {
                    index++;
                
                    if (!visibleRect.Intersects(e.EventPosition.ToRect()))
                        continue;
                
                    double yPos = e.Position.Y;

                    var ft = NumberCache.Get(index);
                    context.DrawText(ft, new Point(PaddingLeft, yPos + 5));
                    DrawProblems(context, index, yPos);
                }
                else
                {
                    foreach (var a in e.Actions)
                    {
                        index++;
                        
                        if (a.Id == SmartConstants.ActionComment && HideComments)
                            continue;

                        if (!visibleRect.Intersects(a.Position.ToRect()))
                            continue;
                    
                        double yPos = a.Position.Y;

                        var ft = NumberCache.Get(index);
                        context.DrawText(ft, new Point(PaddingLeft, yPos + 5));
                        DrawProblems(context, index, yPos);
                    }
                }
            }
        }
    }
    
    private void DrawProblems(DrawingContext dc, int index, double yPos)
    {
        if (Problems != null && Problems.TryGetValue(index, out var severity))
        { 
            vvvvText!.SetForegroundBrush(severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical ? Brushes.Red : Brushes.Orange);
            dc.DrawText(vvvvText, new Point(PaddingLeft, yPos + 5 + 10));   
        }
    }

}