using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using JetBrains.Profiler.Api;
using WDE.Common.Avalonia;
using Prism.Events;
using WDE.Common.Avalonia.Debugging;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Database;
using WDE.Common.Debugging;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.MVVM.Utils;
using WDE.SmartScriptEditor.Avalonia.Debugging;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Debugging;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.ViewModels;
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

    public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<VirtualizedSmartScriptPanel, Control, bool>("Selected");
    public static bool GetSelected(Control control) => (bool?)control.GetValue(SelectedProperty) ?? false;
    public static void SetSelected(Control control, bool value) => control.SetValue(SelectedProperty, value);

    public static readonly AvaloniaProperty DropItemsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropItems));

    public ICommand DropItems
    {
        get => (ICommand?) GetValue(DropItemsProperty) ?? AlwaysDisabledCommand.Command;
        set => SetValue(DropItemsProperty, value);
    }

    public static readonly AvaloniaProperty DropGroupsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropGroups));

    public ICommand DropGroups
    {
        get => (ICommand?) GetValue(DropGroupsProperty) ?? AlwaysDisabledCommand.Command;
        set => SetValue(DropGroupsProperty, value);
    }

    public static readonly AvaloniaProperty DropActionsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropActions));

    public ICommand DropActions
    {
        get => (ICommand?) GetValue(DropActionsProperty) ?? AlwaysDisabledCommand.Command;
        set => SetValue(DropActionsProperty, value);
    }

    public static readonly AvaloniaProperty DropConditionsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, ICommand>(nameof(DropConditions));

    public ICommand DropConditions
    {
        get => (ICommand?) GetValue(DropConditionsProperty) ?? AlwaysDisabledCommand.Command;
        set => SetValue(DropConditionsProperty, value);
    }

    public Dictionary<int, DiagnosticSeverity>? Problems
    {
        get => (Dictionary<int, DiagnosticSeverity>?) GetValue(ProblemsProperty);
        set => SetValue(ProblemsProperty, value);
    }
    public static readonly AvaloniaProperty ProblemsProperty = AvaloniaProperty.Register<VirtualizedSmartScriptPanel, Dictionary<int, DiagnosticSeverity>?>(nameof(Problems));

    public IScriptBreakpoints? Breakpoints
    {
        get => (IScriptBreakpoints?)GetValue(BreakpointsProperty);
        set => SetValue(BreakpointsProperty, value);
    }

    public static readonly AvaloniaProperty BreakpointsProperty =
        AvaloniaProperty.Register<VirtualizedSmartScriptPanel, IScriptBreakpoints?>(nameof(Breakpoints));

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
    public const double BreakpointsMargin = EventPaddingLeft + PaddingLeft;
    private const double ActionSpacing = 2;
    private const double ConditionSpacing = 2;
    private const double BreakpointRadius = 8;
    private static readonly Thickness Padding = new (PaddingLeft, PaddingTop, PaddingRight, PaddingBottom);
    private double GroupSpacing => 4;
    private double EventSpacing => compactView ? 2 : 10;
    private double VariableSpacing => 2;
    private VirtualizedSmartScriptPanelRenderOverlay renderOverlay;
    private NoArrangePanel childrenContainer;

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

        childrenContainer = new NoArrangePanel();
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

    private ScrollViewer ScrollView => this.FindAncestorOfType<ScrollViewer>()!;
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
    private IScriptBreakpoints? attachedBreakpoints;
    private System.IDisposable? attachedBreakpointPopupRequest;
    private System.IDisposable? attachedScrollViewer;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Debug.Assert(attachedScript == null);
        if (script is SmartScriptBase newScript)
        {
            newScript.InvalidateVisual += InvalidateScript;
            Dispatcher.UIThread.Post(InvalidateScript); // <- not sure why it is needed. Without it sometimes the script is not rendered initially
            attachedScript = newScript;
        }

        if (Breakpoints is { } breakpoints)
        {
            breakpoints.BreakpointsModified += InvalidateScript;
            attachedBreakpointPopupRequest = ViewBind.ResolveViewModel<IEventAggregator>().GetEvent<IdeBreakpointRequestPopupEvent>().Subscribe(OnPopupOpenRequest);
            attachedBreakpoints = breakpoints;
        }
        if (ScrollView is { } sc)
        {
            var a = sc.GetObservable(ScrollViewer.OffsetProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
                InvalidateVisual();
            });
            var b = sc.GetObservable(ScrollViewer.ViewportProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
                InvalidateVisual();
            });
            attachedScrollViewer = new CompositeDisposable(a, b);
        }
    }

    private void OnPopupOpenRequest(IdeBreakpointRequestPopupEventArgs obj)
    {
        if (script is not SmartScript { } s)
            return;

        if (Breakpoints is not { } breakpoints)
            return;

        if (breakpoints.GetElement(obj.HitDebugPoint) is { } element)
        {
            if (element is SmartEvent ev)
            {
                obj.Handled = true;
                obj.AttachPopupToObject = this;
                obj.PopupOffsetX = ev.EventPosition.X;
                obj.PopupOffsetY = ev.EventPosition.Bottom;
            }
            else if (element is SmartAction a)
            {
                obj.Handled = true;
                obj.AttachPopupToObject = this;
                obj.PopupOffsetX = a.Position.X + 50; // 50 offset to make it more look like it is attached to the action
                obj.PopupOffsetY = a.Position.Bottom;
            }
            else if (element is SmartSource source && source.Parent is { } sourceParent)
            {
                obj.Handled = true;
                obj.AttachPopupToObject = this;
                obj.PopupOffsetX = sourceParent.Position.X;
                obj.PopupOffsetY = sourceParent.Position.Bottom;
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (attachedScript is { })
            attachedScript.InvalidateVisual -= InvalidateScript;
        if (attachedBreakpoints is { })
            attachedBreakpoints.BreakpointsModified -= InvalidateScript;
        attachedBreakpointPopupRequest?.Dispose();
        attachedBreakpointPopupRequest = null;
        attachedScript = null;
        attachedBreakpoints = null;
        attachedScrollViewer?.Dispose();
        attachedScrollViewer = null;
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
        var systemWideControlModifier = KeyGestures.CommandModifier;
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

    private void CollectSmartElementBreakpointsAtPoint(Point point, List<SmartBaseElement> outputList)
    {
        var smartDataManager = ViewBind.ResolveViewModel<ISmartDataManager>();
        foreach (var tuple in ScriptIterator)
        {
            if (!tuple.groupIsExpanded)
                continue;

            if (tuple.@event == null)
                continue;

            var eventDestinationId = tuple!.@event!.DestinationEventId;

            var eventBreakPointRect = GetBreakpointRect(tuple!.@event);
            if (eventBreakPointRect.Contains(point) &&
                eventDestinationId.HasValue)
            {
                outputList.Add(tuple!.@event);
            }

            foreach (var action in tuple!.@event!.Actions)
            {
                if (action.Id == SmartConstants.ActionComment && HideComments)
                    continue;

                var breakPointRect = GetBreakpointRect(action);

                if (breakPointRect.Contains(point))
                {
                    if (action.DestinationEventId is { } eventId)
                    {
                        outputList.Add(action);
                        outputList.Add(action.Source);
                        if (!smartDataManager.TryGetRawData(SmartType.SmartAction, action.Id, out var data) || data.TargetTypes != 0)
                            outputList.Add(action.Target);
                    }
                    break;
                }
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        UpdateIsCopying(e.KeyModifiers);
        var point = e.GetPosition(this);
        var rightMouseButton = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        var leftMouseButton = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;

        if (e.ClickCount != 1 || script == null)
            return;

        foreach (SmartEvent ev in script.Events)
            ev.IsSelected = false;

        if (Breakpoints == null ||
            point.X > PaddingLeft + EventPaddingLeft)
            return;

        List<SmartBaseElement> possibleTypes = new();
        CollectSmartElementBreakpointsAtPoint(point, possibleTypes);

        if (possibleTypes.Count == 0)
            return;

        if (possibleTypes.Any(x => Breakpoints.GetBreakpoint(x).HasValue))
        {
            if (rightMouseButton)
            {
                var placedBreakpoints = new List<(SmartBaseElement, DebugPointId)>();
                foreach (var possibleType in possibleTypes)
                {
                    var placedBreakpoint = Breakpoints.GetBreakpoint(possibleType);
                    if (placedBreakpoint.HasValue)
                        placedBreakpoints.Add((possibleType, placedBreakpoint.Value));
                }

                if (placedBreakpoints.Count > 0)
                {
                    ViewBind.ResolveViewModel<IEditDebugPointService>().EditDebugPointInPopup(this, placedBreakpoints.Select(x => x.Item2).ToArray()).ListenErrors();
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                foreach (var possible in possibleTypes)
                {
                    Breakpoints.RemoveBreakpoint(possible).ListenErrors(ViewBind.ResolveViewModel<IMessageBoxService>());
                }
            }
        }
        else
        {
            var addBreakpointCommand = new AsyncAutoCommand<SmartBaseElement>(async element =>
            {
                if (rightMouseButton)
                {
                    var debugPoint = await Breakpoints.AddBreakpoint(element, SmartBreakpointFlags.None, null, true);
                    await ViewBind.ResolveViewModel<IEditDebugPointService>().EditDebugPointInPopup(this, debugPoint);
                    await Breakpoints.Synchronize(element);
                }
                else
                {
                    await Breakpoints.AddBreakpoint(element, SmartBreakpointFlags.None, null);
                }
            }).WrapMessageBox<Exception, SmartBaseElement>(ViewBind.ResolveViewModel<IMessageBoxService>());

            var addAllBreakpointCommand = new AsyncAutoCommand(async () =>
            {
                if (rightMouseButton)
                {
                    List<DebugPointId> debugPoints = new();
                    foreach (var pos in possibleTypes)
                        debugPoints.Add(await Breakpoints.AddBreakpoint(pos, SmartBreakpointFlags.None, null));

                    await ViewBind.ResolveViewModel<IEditDebugPointService>().EditDebugPointInPopup(this, debugPoints.ToArray());

                    foreach (var pos in possibleTypes)
                        await Breakpoints.Synchronize(pos);
                }
                else
                {
                    foreach (var pos in possibleTypes)
                        await Breakpoints.AddBreakpoint(pos, SmartBreakpointFlags.None, null);
                }
            }).WrapMessageBox<Exception>(ViewBind.ResolveViewModel<IMessageBoxService>());

            if (possibleTypes.Count > 1)
            {
                var menu = new BreakpointMenuViewModel();
                foreach (var element in possibleTypes)
                {
                    menu.MenuItems.Add(new BreakpointMenuItemViewModel(element, addBreakpointCommand));
                }
                menu.MenuItems.Add(new BreakpointMenuItemViewModel(SmartBreakpointType.Any, "All", addAllBreakpointCommand, null));

                var popup = new BreakpointMenuView()
                {
                    DataContext = menu
                };
                popup.Open(this);
            }
            else if (possibleTypes.Count == 1)
            {
                addBreakpointCommand.Execute(possibleTypes[0]);
            }
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

        if (mouseX < PaddingLeft + EventPaddingLeft + 50) // 50 is extra margin
            InvalidateVisual(); // hover breakpoint might have changed

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

        UpdateBreakpointTooltip();
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

    private (double y, double height, int actionIndex, int eventIndex)? GetActionFromY()
    {
        bool found = false;
        foreach (var tuple in ScriptIterator)
        {
            if (tuple is (null, var ev, var inGroup, var groupExpanded, var eventIndex))
            {
                if (!groupExpanded)
                    continue;

                if (ev == null)
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
                        return overTuple;
                    }

                    actionIndex++;
                }

                if (found)
                    break;

                var rest = (y: ev.Position.Y + actionsHeights + (ev.Position.Height - actionsHeights) / 2, ev.Position.Height - actionsHeights, ev.Actions.Count, eventIndex);
                if (rest.y > mouseY)
                {
                    return rest;
                }
            }
        }

        return null;
    }

    private void UpdateBreakpointTooltip()
    {
        void DisableTooltip()
        {
            if (ToolTip.GetIsOpen(this))
            {
                ToolTip.SetIsOpen(this, false);
            }
            ToolTip.SetTip(this, null);
        }
        if (mouseX >= PaddingLeft + EventPaddingLeft)
        {
            DisableTooltip();
            return;
        }

        if (script == null)
            return;

        var breakpoints = Breakpoints;

        if (breakpoints == null)
            return;

        List<string>? breakpointsText = null;

        string GetBreakpointStateText(BreakpointState state)
        {
            if (!breakpoints!.IsConnected)
                return "The editor is not connected to the server. The breakpoint will be synced once you start the server.";

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

        foreach (var tuple in ScriptIterator)
        {
            if (!tuple.groupIsExpanded)
                continue;

            if (tuple.@event == null)
                continue;

            var eventBreakpointRect = GetBreakpointRect(tuple.@event!);
            if (eventBreakpointRect.Contains(new Point(mouseX, mouseY)))
            {
                if (breakpoints.GetBreakpoint(tuple.@event!) is { } eventBreakpoint)
                {
                    breakpointsText ??= new();
                    breakpointsText.Add($"{tuple.@event!.Readable.RemoveTags()}\n{GetBreakpointStateText(breakpoints.GetState(eventBreakpoint))}");
                }
            }

            foreach (var action in tuple.@event!.Actions)
            {
                if (action.Id == SmartConstants.ActionComment &&
                    hideComments)
                    continue;

                var actionBreakpointRect = GetBreakpointRect(action);
                if (actionBreakpointRect.Contains(new Point(mouseX, mouseY)))
                {
                    var actionReadable = action.Readable.RemoveTags();
                    var sourceReadable = action.Source.Readable.RemoveTags();

                    if (breakpoints.GetBreakpoint(action) is { } actionBreakpoint)
                    {
                        breakpointsText ??= new();
                        actionReadable = actionReadable.Replace(sourceReadable + ": ", "");
                        breakpointsText.Add($"{actionReadable}\n{GetBreakpointStateText(breakpoints.GetState(actionBreakpoint))}");
                    }

                    if (breakpoints.GetBreakpoint(action.Source) is { } sourceBreakpoint)
                    {
                        breakpointsText ??= new();
                        breakpointsText.Add($"{sourceReadable}\n{GetBreakpointStateText(breakpoints.GetState(sourceBreakpoint))}");
                    }

                    if (breakpoints.GetBreakpoint(action.Target) is { } targetBreakpoint)
                    {
                        var targetReadable = action.Target.Readable.RemoveTags();
                        breakpointsText ??= new();
                        breakpointsText.Add($"{targetReadable}\n{GetBreakpointStateText(breakpoints.GetState(targetBreakpoint))}");
                    }
                }
            }
        }

        if (breakpointsText == null)
        {
            DisableTooltip();
        }
        else
        {
            ToolTip.SetTip(this, string.Join("\n\n", breakpointsText));
            ToolTip.SetIsOpen(this, true);
        }
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
            if (GetActionFromY() is { } action)
                overIndexAction = action;

            InvalidateVisual();
        }
        else if (draggingConditions)
        {
            bool found = false;
            if (!hideConditions)
            {
                foreach (var tuple in ScriptIterator)
                {
                    if (tuple is (null, var ev, var _, var groupExpanded, var eventIndex))
                    {
                        if (!groupExpanded)
                            continue;
                    
                        int conditionIndex = 0;
                        double conditionsHeight = 0;
                        foreach (var condition in ev!.Conditions)
                        {
                            var overTuple = (y: condition.Position.Y + (condition.Position.Height + ActionSpacing) / 2, condition.Position.Height + ActionSpacing, conditionIndex: conditionIndex, eventIndex);
                            conditionsHeight += condition.Position.Height + ActionSpacing;
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
                
                        var rest = (y: ev.Position.Y + (ev.CachedHeight ?? 0) + conditionsHeight + (ev.Position.Height - conditionsHeight) / 2, ev.Position.Height - conditionsHeight, ev.Conditions.Count, eventIndex);
                        if (rest.y > mouseY)
                        {
                            overIndexCondition = rest;
                            break;
                        }
                    }
                }   
            }
            InvalidateVisual();
        }
    }

    private bool CanDragConditions()
    {
        return script != null && script.EditorFeatures.CanReorderConditions;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        mouseStartPositionValid = false;
        mouseY = -1;
        mouseY = -1;
        InvalidateVisual();
    }

    #endregion
}
