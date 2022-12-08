using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using WDE.SmartScriptEditor.Avalonia.Extensions;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public class SmartScriptOverlay : Control
{
    private SmartScript? script;
    public static readonly DirectProperty<SmartScriptOverlay, SmartScript?> ScriptProperty = AvaloniaProperty.RegisterDirect<SmartScriptOverlay, SmartScript?>("Script", o => o.Script, (o, v) => o.Script = v);
    public static readonly StyledProperty<string?> FindTextProperty = AvaloniaProperty.Register<SmartScriptOverlay, string?>("FindText");

    public SmartScript? Script
    {
        get => script;
        set => SetAndRaise(ScriptProperty, ref script, value);
    }
    
    public string? FindText
    {
        get => (string?)GetValue(FindTextProperty);
        set => SetValue(FindTextProperty, value);
    }

    private ScrollViewer ScrollView => this.FindAncestorOfType<ScrollViewer>();
    private InverseRenderTransformPanel? Panel => this.FindAncestorOfType<InverseRenderTransformPanel>();

    private Rect VisibleRect
    {
        get
        {
            var rect = UnscaledVisibleRect;
            var scaler = Panel;
            var scaleX = scaler?.RenderTransform?.Value.M11 ?? 1;
            var scaleY = scaler?.RenderTransform?.Value.M22 ?? 1;
            return new Rect(rect.X / scaleX, rect.Y / scaleY, rect.Width / scaleX, rect.Height / scaleY);
        }
    }
    private Rect UnscaledVisibleRect
    {
        get
        {
            var scrollViewer = ScrollView;
            return new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height);
        }
    }

    static SmartScriptOverlay()
    {  
        IsHitTestVisibleProperty.OverrideDefaultValue<SmartScriptOverlay>(false);
        AffectsRender<SmartScriptOverlay>(FindTextProperty);
        ScriptProperty.Changed.AddClassHandler<SmartScriptOverlay>((panel, e) =>
        {
            if (e.OldValue is SmartScript oldScript)
            {
                oldScript.EventChanged -= panel.EventChanged;
            }
            if (e.NewValue is SmartScript newScript)
            {
                newScript.EventChanged += panel.EventChanged;
            }
        });
    }

    private void EventChanged(SmartEvent? arg1, SmartAction? arg2, EventChangedMask arg3)
    {
        if (!string.IsNullOrWhiteSpace(FindText))
            InvalidateVisual();
    }
    
    public override void Render(DrawingContext context)
    {
        if (script == null || string.IsNullOrWhiteSpace(FindText))
            return;
        
        var scrollRect = VisibleRect;
        Geometry full = new RectangleGeometry(scrollRect);
        var group = new GeometryGroup();
        
        bool any = false;
        bool inGroup = false;
        bool groupIsHidden = false;
        foreach (var e in script.Events)
        {
            if (e.IsBeginGroup)
            {
                inGroup = true;
                var eventGroup = new SmartGroup(e);
                groupIsHidden = !eventGroup.IsExpanded;
                var groupRect = eventGroup.Position.ToRect();
                if (scrollRect.Intersects(groupRect))
                {
                    if (eventGroup.Header.Contains(FindText, StringComparison.InvariantCultureIgnoreCase) ||
                        (eventGroup.Description?.Contains(FindText, StringComparison.InvariantCultureIgnoreCase) ?? false))
                    {
                        group.Children!.Add(new RectangleGeometry(groupRect));
                        any = true;
                    }
                }
            }
            else if (e.IsEndGroup)
            {
                inGroup = false;
                groupIsHidden = false;
            }
            else if (e.IsEvent)
            {
                if (inGroup && groupIsHidden)
                    continue;
                
                var eventRect = e.EventPosition.ToRect();
                if (scrollRect.Intersects(eventRect))
                {
                    if (e.Readable.Contains(FindText, StringComparison.InvariantCultureIgnoreCase))
                    {
                        group.Children!.Add(new RectangleGeometry(eventRect));
                        any = true;
                    }
                }
            
                foreach (var action in e.Actions)
                {
                    var actionRect = action.Position.ToRect();
                    if (scrollRect.Intersects(actionRect))
                    {
                        if (action.Readable.Contains(FindText, StringComparison.InvariantCultureIgnoreCase))
                        {
                            group.Children!.Add(new RectangleGeometry(actionRect));
                            any = true;
                        }
                    }
                }
            
                foreach (var condition in e.Conditions)
                {
                    var actionRect = condition.Position.ToRect();
                    if (scrollRect.Intersects(actionRect))
                    {
                        if (condition.Readable.Contains(FindText, StringComparison.InvariantCultureIgnoreCase))
                        {
                            group.Children!.Add( new RectangleGeometry(actionRect));
                            any = true;
                        }
                    }
                }   
            }
        }

        if (any)
        {
            var result = new CombinedGeometry(GeometryCombineMode.Exclude, full, group);
            context.DrawGeometry(new SolidColorBrush(new Color(150, 0,0, 0), 1), null, result);
        }
        else
        {
            context.DrawGeometry(new SolidColorBrush(new Color(150, 0,0, 0), 1), null, full);
        }
    }
}