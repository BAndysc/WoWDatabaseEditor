using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using JetBrains.Profiler.Api;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Managers;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Settings;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public partial class VirtualizedSmartScriptPanel : Panel
{
    private SmartScript? script;
    public static readonly DirectProperty<VirtualizedSmartScriptPanel, SmartScript?> ScriptProperty = AvaloniaProperty.RegisterDirect<VirtualizedSmartScriptPanel, SmartScript?>("Script", o => o.Script, (o, v) => o.Script = v);
    public static readonly StyledProperty<IDataTemplate> EventItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(EventItemTemplate));
    public static readonly StyledProperty<IDataTemplate> ActionItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(ActionItemTemplate));
    public static readonly StyledProperty<IDataTemplate> ConditionItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(ConditionItemTemplate));
    public static readonly StyledProperty<IDataTemplate> VariableItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(VariableItemTemplate));
    public static readonly StyledProperty<IDataTemplate> GroupItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(GroupItemTemplate));
    public static readonly StyledProperty<IDataTemplate> NewActionItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(NewActionItemTemplate));
    public static readonly StyledProperty<IDataTemplate> NewConditionItemTemplateProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IDataTemplate>(nameof(NewConditionItemTemplate));

    public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<VirtualizedSmartScriptPanel, IControl, bool>("Selected");
    public static bool GetSelected(IControl control) => (bool)control.GetValue(SelectedProperty);
    public static void SetSelected(IControl control, bool value) => control.SetValue(SelectedProperty, value);

    public static readonly AvaloniaProperty DropItemsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropItems));

    public ICommand DropItems
    {
        get => (ICommand) GetValue(DropItemsProperty);
        set => SetValue(DropItemsProperty, value);
    }

    public static readonly AvaloniaProperty DropGroupsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropGroups));

    public ICommand DropGroups
    {
        get => (ICommand) GetValue(DropGroupsProperty);
        set => SetValue(DropGroupsProperty, value);
    }

    public static readonly AvaloniaProperty DropActionsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropActions));

    public ICommand DropActions
    {
        get => (ICommand) GetValue(DropActionsProperty);
        set => SetValue(DropActionsProperty, value);
    }

    public static readonly AvaloniaProperty DropConditionsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropConditions));

    public ICommand DropConditions
    {
        get => (ICommand) GetValue(DropConditionsProperty);
        set => SetValue(DropConditionsProperty, value);
    }
    
    public Dictionary<int, DiagnosticSeverity>? Problems
    {
        get => (Dictionary<int, DiagnosticSeverity>?) GetValue(ProblemsProperty);
        set => SetValue(ProblemsProperty, value);
    }
    public static readonly AvaloniaProperty ProblemsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, Dictionary<int, DiagnosticSeverity>?>(nameof(Problems));
    
    private bool hideComments;
    public static readonly DirectProperty<VirtualizedSmartScriptPanel, bool> HideCommentsProperty = AvaloniaProperty.RegisterDirect<VirtualizedSmartScriptPanel, bool>("HideComments", o => o.HideComments, (o, v) => o.HideComments = v);

    public bool HideComments
    {
        get => hideComments;
        set => SetAndRaise(HideCommentsProperty, ref hideComments, value);
    }
    
    private bool hideConditions;
    public static readonly DirectProperty<VirtualizedSmartScriptPanel, bool> HideConditionsProperty = AvaloniaProperty.RegisterDirect<VirtualizedSmartScriptPanel, bool>("HideConditions", o => o.HideConditions, (o, v) => o.HideConditions = v);
    public bool HideConditions
    {
        get => hideConditions;
        set => SetAndRaise(HideConditionsProperty, ref hideConditions, value);
    }

    public static readonly DirectProperty<VirtualizedSmartScriptPanel, double> EventsWidthProperty = AvaloniaProperty.RegisterDirect<VirtualizedSmartScriptPanel, double>(nameof(EventsWidth), o => o.EventsWidth);
    public double EventsWidth
    {
        get => EventWidth(Bounds.Width).Width;
    }
    
    private readonly bool compactView;
    private const double PaddingLeft = 5;
    private const double PaddingBottom = 5;
    private const double PaddingRight = 5;
    private const double PaddingTop = 5;
    private const double EventPaddingLeft = 20;
    private const double ActionSpacing = 2;
    private const double ConditionSpacing = 2;
    private static readonly Thickness Padding = new (PaddingLeft, PaddingTop, PaddingRight, PaddingBottom);
    private double GroupSpacing => 4;
    private double EventSpacing => compactView ? 2 : 10;
    private double VariableSpacing => 2;
    private VirtualizedSmartScriptPanelRenderOverlay renderOverlay;

    private static HorizRect ConditionWidth(double totalWidth)
    {
        var eventRect = EventWidth(totalWidth);
        var x = eventRect.X + 21;
        var width = Math.Max(eventRect.Width - 22, 0);
        return new HorizRect(x, width);
    }

    private static HorizRect EventWidth(double totalWidth)
    {
        var x = PaddingLeft + EventPaddingLeft;
        var right = Math.Min(Math.Max(totalWidth - 50, PaddingLeft + EventPaddingLeft + 10), 350);
        return new HorizRect(x, right - x);
    }
    
    static VirtualizedSmartScriptPanel()
    {
        HideCommentsProperty.Changed.AddClassHandler<VirtualizedSmartScriptPanel>((panel, _) => panel.InvalidateScript());
        HideConditionsProperty.Changed.AddClassHandler<VirtualizedSmartScriptPanel>((panel, _) => panel.InvalidateScript());
        ScriptProperty.Changed.AddClassHandler<VirtualizedSmartScriptPanel>((panel, e) =>
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
        BoundsProperty.Changed.AddClassHandler<VirtualizedSmartScriptPanel>((panel, e) =>
        {
            panel.RaisePropertyChanged(EventsWidthProperty, default, panel.EventsWidth);
        });
    }
    

    public VirtualizedSmartScriptPanel()
    {
        var smartScriptSettings = ViewBind.ResolveViewModel<IGeneralSmartScriptSettingsProvider>();
        compactView = smartScriptSettings.ViewType == SmartScriptViewType.Compact;

        var childrenContainer = new Panel();
        renderOverlay = new VirtualizedSmartScriptPanelRenderOverlay(this);

        // firstly children, then overlay
        Children.Add(childrenContainer);
        Children.Add(renderOverlay);

        measureGroup = new MeasureUtil(this);
        measureCondition = new MeasureUtil(this);
        measureGlobalVariable = new MeasureUtil(this);
        measureEvent = new MeasureUtil(this);
        
        eventViews = new(childrenContainer, true);
        actionViews = new(childrenContainer);
        conditionViews = new(childrenContainer);
        variableViews = new(childrenContainer);
        commentsViews = new(childrenContainer);
        groupViews = new(childrenContainer);
        newActionViews = new(childrenContainer);
        newConditionViews = new(childrenContainer);
        textBlockViews = new(childrenContainer);
    }

    private void EventChanged(SmartEvent? arg1, SmartAction? arg2, EventChangedMask arg3)
    {
        InvalidateScript();
    }

    public SmartScript? Script
    {
        get => script;
        set => SetAndRaise(ScriptProperty, ref script, value);
    }

    public IDataTemplate EventItemTemplate
    {
        get => GetValue(EventItemTemplateProperty);
        set => SetValue(EventItemTemplateProperty, value);
    }

    public IDataTemplate ActionItemTemplate
    {
        get => GetValue(ActionItemTemplateProperty);
        set => SetValue(ActionItemTemplateProperty, value);
    }

    public IDataTemplate ConditionItemTemplate
    {
        get => GetValue(ConditionItemTemplateProperty);
        set => SetValue(ConditionItemTemplateProperty, value);
    }

    public IDataTemplate VariableItemTemplate
    {
        get => GetValue(VariableItemTemplateProperty);
        set => SetValue(VariableItemTemplateProperty, value);
    }

    public IDataTemplate GroupItemTemplate
    {
        get => GetValue(GroupItemTemplateProperty);
        set => SetValue(GroupItemTemplateProperty, value);
    }
    
    public IDataTemplate NewActionItemTemplate
    {
        get => GetValue(NewActionItemTemplateProperty);
        set => SetValue(NewActionItemTemplateProperty, value);
    }

    public IDataTemplate NewConditionItemTemplate
    {
        get => GetValue(NewConditionItemTemplateProperty);
        set => SetValue(NewConditionItemTemplateProperty, value);
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

    private void InvalidateScript()
    {
        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();
    }

    private SmartScriptBase? attachedScript;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Debug.Assert(attachedScript == null);
        if (script is SmartScriptBase newScript)
        {
            newScript.InvalidateVisual += InvalidateScript;
            attachedScript = newScript;
        }
        if (ScrollView is { } sc)
        {
            sc.GetObservable(ScrollViewer.OffsetProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
            });
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (attachedScript is { })
            attachedScript.InvalidateVisual -= InvalidateScript;
        attachedScript = null;
        base.OnDetachedFromVisualTree(e);
        MeasureProfiler.SaveData();
    }

    #region Drag n Drop

    private bool AnyDragging => draggingGroups || draggingEvents || draggingActions || draggingConditions;
    private bool draggingActions;
    private bool draggingEvents;
    private bool draggingGroups;
    private bool draggingConditions;
    private bool isCopying;

    private Point mouseStartPosition;
    private float mouseX, mouseY;

    private (double y, int eventIndex) overIndexGroup;
    public (double y, int eventIndex, bool inGroup) OverIndexEvent { get; set; }
    private (double y, double height, int conditionIndex, int eventIndex) overIndexCondition;
    private (double y, double height, int actionIndex, int eventIndex) overIndexAction;
    
    private void UpdateIsCopying(KeyModifiers key)
    {
        var systemWideControlModifier = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
        isCopying = key.HasFlagFast(systemWideControlModifier);
    }

    private bool AnythingSelected()
    {
        if (script == null)
            return false;
        
        foreach (var e in script.Events)
        {
            if (e.IsSelected)
                return true;
        }
        foreach (var e in script.Events)
        {
            foreach (var a in e.Actions)
                if (a.IsSelected)
                    return true;
            foreach (var a in e.Conditions)
                if (a.IsSelected)
                    return true;
        }

        return false;
    }


    private void StopDragging()
    {
        if (AnythingSelected())
        {
            if (draggingGroups)
                DropGroups?.Execute(new DropActionsConditionsArgs{EventIndex = overIndexGroup.eventIndex, ActionIndex = 0, Copy = isCopying});
            else if (draggingEvents)
                DropItems?.Execute(new DropActionsConditionsArgs{EventIndex = OverIndexEvent.eventIndex, ActionIndex = 0, Copy = isCopying});
            else if (draggingActions)
            {
                DropActions?.Execute(new DropActionsConditionsArgs
                    {EventIndex = overIndexAction.eventIndex, ActionIndex = overIndexAction.actionIndex, Copy = isCopying});
            }
            else if (draggingConditions)
            {
                DropConditions?.Execute(new DropActionsConditionsArgs
                    {EventIndex = overIndexCondition.eventIndex, ActionIndex = overIndexCondition.conditionIndex, Copy = isCopying});
            }
        }

        draggingEvents = false;
        draggingActions = false;
        draggingConditions = false;
        draggingGroups = false;
        InvalidateScript();
    }

    private new void InvalidateVisual()
    {
        renderOverlay.InvalidateVisual();
    }

    private IEnumerable<(SmartGroup? group, SmartEvent? @event, bool inGroup, bool groupIsExpanded, int eventIndex)> ScriptIterator
    {
        get
        {
            if (script == null)
                yield break;

            int eventIndex = -1;
            bool inGroup = false;
            bool groupExpanded = true;
            foreach (var e in script.Events)
            {
                eventIndex++;

                if (e.IsBeginGroup)
                {
                    var group = new SmartGroup(e);
                    groupExpanded = group.IsExpanded;
                    yield return (group, null, false, groupExpanded, eventIndex);
                    inGroup = true;
                }
                else if (e.IsEndGroup)
                {
                    inGroup = false;
                    groupExpanded = true;
                }
                else
                {
                    if (inGroup && !groupExpanded)
                        continue;
                    
                    yield return (null, e, inGroup, groupExpanded, eventIndex);
                }
            }
        }
    }

    private bool AnyActionSelected() => 
        script != null && script.Events.SelectMany(e => e.Actions).Any(a => a.IsSelected);

    private bool AnyEventSelected() => 
        script != null && script.Events.Any(a => a.IsSelected && a.IsEvent);

    private bool AnyGroupSelected() =>
        script != null && script.Events.Any(a => a.IsSelected && a.IsBeginGroup);

    private bool mouseStartPositionValid = false;
    private bool leftMouseButtonPressed = false;
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        UpdateIsCopying(e.KeyModifiers);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount == 1 && script != null)
        {
            foreach (SmartEvent ev in script.Events)
                ev.IsSelected = false;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        UpdateIsCopying(e.KeyModifiers);
        StopDragging();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (script == null)
            return;

        UpdateIsCopying(e.KeyModifiers);
        var state = e.GetCurrentPoint(this);

        var point = e.GetPosition(this);
        mouseX = (float) point.X;
        mouseY = (float) point.Y;
        if (!state.Properties.IsLeftButtonPressed)
        {
            mouseStartPosition = e.GetPosition(this);
            mouseStartPositionValid = true;
        }
        if (mouseStartPositionValid && 
            state.Properties.IsLeftButtonPressed &&
            !AnyDragging)
        {
            var d = new Point(mouseStartPosition.X - e.GetPosition(this).X, mouseStartPosition.Y - e.GetPosition(this).Y);
            var dist = d.X * d.X + d.Y * d.Y;
            if (dist > 10 * 10)
            {
                if (e.GetPosition(this).X < EventWidth(Bounds.Width).Right)
                {
                    if (AnyGroupSelected())
                        draggingGroups = true;
                    else if (AnyEventSelected())
                        draggingEvents = true;
                    else if (CanDragConditions())
                        draggingConditions = true;
                }
                else
                    draggingActions = AnyActionSelected();
        
                if (AnyDragging)
                {
                    mouseStartPositionValid = false;
                    //CaptureMouse();
                }
            }
        }

        UpdateOverElement();

        // scroll 
        ScrollOnEdges();
        InvalidateArrange();
    }

    private void ScrollOnEdges()
    {
        // scroll when dragging on edges
        if (AnyDragging)
        {
            if (ScrollView is { } scrollViewer && Panel is { } panel)
            {
                var scale = panel.RenderTransform?.Value.M11 ?? 1;
                var relativeMouse = mouseY * scale - scrollViewer.Offset.Y;
                var offset = 0;
                if (relativeMouse < 15)
                    offset = -1;
                else if (relativeMouse > scrollViewer.Viewport.Height - 15)
                    offset = 1;

                if (offset != 0)
                {
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y + offset * 10);
                }
            }
        }
    }

    private bool TryGetEvent(int index, out SmartEvent e)
    {
        e = null!;
        if (script != null && index >= 0 && index < script.Events.Count)
        {
            e = script.Events[index];
            return true;
        }

        return false;
    }

    private void UpdateOverElement()
    {
        if (draggingGroups)
        {
            var found = false;
            foreach (var tuple in ScriptIterator)
            {
                if (tuple is (null, var ev, var inGroup, var groupExpanded, var eventIndex))
                {
                    if (inGroup)
                        continue;
                    
                    double height = ev!.Position.Height;
                    if (mouseY < ev.Position.Y + height / 2)
                    {
                        overIndexGroup = (y: ev.Position.Y - EventSpacing / 2, eventIndex);
                        found = true;
                        break;
                    }
                    if (mouseY < ev.Position.Y + height)
                    {
                        overIndexGroup = (y: ev.Position.Y + height + EventSpacing / 2, eventIndex + 1);
                        found = true;
                        break;
                    }
                }
                else if (tuple is (var group, null, var inGroup2, var groupExpanded2, var eventIndex2))
                {
                    double height = group!.Position.Height;
                    if (mouseY < group.Position.Y + height / 2)
                    {
                        overIndexGroup = (group.Position.Y - EventSpacing / 2, eventIndex2);
                        found = true;
                        break;
                    }
                }
            }
        
            if (!found && script!.Events.Count >= 1)
            {
                var lastEvent = script.Events[^1];
                overIndexGroup = (lastEvent.Position.Bottom, script.Events.Count);
            }
            InvalidateVisual();
        }
        else if (draggingEvents)
        {
            var found = false;
            foreach (var tuple in ScriptIterator)
            {
                if (tuple is (null, var ev, var inGroup, var groupExpanded, var eventIndex))
                {
                    if (!groupExpanded)
                        continue;
                    
                    double height = ev!.Position.Height;
                    if (mouseY < ev.Position.Y + height / 2)
                    {
                        OverIndexEvent = (y: ev.Position.Y - EventSpacing / 2, eventIndex, inGroup);
                        found = true;
                        break;
                    }
                    else if (mouseY < ev.Position.Y + height)
                    {
                        OverIndexEvent = (y: ev.Position.Y + height + EventSpacing / 2, eventIndex + 1, inGroup);
                        found = true;
                        break;
                    }
                }
                else if (tuple is (var group, null, var inGroup2, var groupExpanded2, var eventIndex2))
                {
                    double height = group!.Position.Height;
                    /*if (mouseY < group.Position.Y)// && eventIndex2 - 1 < script!.Events.Count && eventIndex2 - 1 >= 0)// && script.Events[eventIndex2 - 1].HasGroup)
                    {
                        // if above this group, there is an event within a group, this position will snap it to the bottom of the previous group
                        OverIndexEvent = (group.Position.Y - EventSpacing, eventIndex2, false);
                        found = true;
                        break;
                    }
                    else */
                    if (mouseY < group.Position.Y + height / 2)
                    {
                        OverIndexEvent = (group.Position.Y - EventSpacing / 2, eventIndex2, false);
                        found = true;
                        break;
                    }
                    // if eventIndex2 + 1 < events count, then there is the next event that will catch this position
                    else if (mouseY < group.Position.Bottom && (eventIndex2 + 1 >= script!.Events.Count || script.Events[eventIndex2 + 1].IsEndGroup))
                    {
                        OverIndexEvent = (group.Position.Y + height, eventIndex2 + 1, true);
                        found = true;
                        break;
                    }
                }
            }
        
            if (!found && script!.Events.Count >= 1)
            {
                var lastEvent = script.Events[^1];
                OverIndexEvent = (lastEvent.Position.Bottom, script.Events.Count, false);
            }
            InvalidateVisual();
        }
        else if (draggingActions)
        {
            bool found = false;
            foreach (var tuple in ScriptIterator)
            {
                if (tuple is (null, var ev, var inGroup, var groupExpanded, var eventIndex))
                {
                    if (!groupExpanded)
                        continue;
                    
                    int actionIndex = 0;
                    double actionsHeights = 0;
                    foreach (var action in ev!.Actions)
                    {
                        if (action.Id == SmartConstants.ActionComment && HideComments)
                            continue;
                        
                        var overTuple = (y: action.Position.Y + (action.Position.Height + ActionSpacing) / 2, action.Position.Height + ActionSpacing, actionIndex, eventIndex);
                        actionsHeights += action.Position.Height + ActionSpacing;
                        if (overTuple.y > mouseY)
                        {
                            overIndexAction = overTuple;
                            found = true;
                            break;
                        }

                        actionIndex++;
                    }

                    if (found)
                        break;
                
                    var rest = (y: ev.Position.Y + actionsHeights + (ev.Position.Height - actionsHeights) / 2, ev.Position.Height - actionsHeights, ev.Actions.Count, eventIndex);
                    if (rest.y > mouseY)
                    {
                        overIndexAction = rest;
                        break;
                    }
                }
            }

            InvalidateVisual();
        }
        else if (draggingConditions)
        {
            bool found = false;
            foreach (var tuple in ScriptIterator)
            {
                if (tuple is (null, var ev, var _, var groupExpanded, var eventIndex))
                {
                    if (!groupExpanded)
                        continue;
                    
                    int conditionIndex = 0;
                    foreach (var condition in ev!.Conditions)
                    {
                        var overTuple = (y: condition.Position.Y + (condition.Position.Height + ActionSpacing) / 2,
                            condition.Position.Height + ActionSpacing, eventIndex, conditionIndex);
                        if (overTuple.y > mouseY)
                        {
                            overIndexCondition = overTuple;
                            found = true;
                            break;
                        }

                        conditionIndex++;
                    }

                    if (found)
                        break;
                }
            }
            InvalidateVisual();
        }
    }

    private bool CanDragConditions()
    {
        return script != null && script.EditorFeatures.CanReorderConditions;
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        base.OnPointerLeave(e);
        mouseStartPositionValid = false;
    }

    #endregion
}
