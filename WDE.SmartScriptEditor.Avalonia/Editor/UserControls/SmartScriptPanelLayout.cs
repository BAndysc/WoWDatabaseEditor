using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    internal class SmartScriptPanelLayout : Panel
    {
        private readonly List<(float y, float height, int actionIndex, int eventIndex)> actionHeights = new();
        private readonly List<(float y, float height, int conditionIndex, int eventIndex)> conditionHeights = new();
        private readonly List<(float y, float height, int eventIndex)> eventHeights = new();
        
        private readonly Dictionary<SmartAction, ContentPresenter> actionToPresenter = new();
        private readonly Dictionary<SmartEvent, ContentPresenter> eventToPresenter = new();
        private readonly Dictionary<SmartCondition, ContentPresenter> conditionToPresenter = new();
        private readonly Dictionary<ContentPresenter, SmartCondition> presenterToCondition = new();
        private readonly Dictionary<ContentPresenter, SmartAction> presenterToAction = new();
        private readonly Dictionary<ContentPresenter, SmartEvent> presenterToEvent = new();
        private readonly Dictionary<GlobalVariable, ContentPresenter> variableToPresenter = new();
        private readonly Dictionary<ContentPresenter, GlobalVariable> presenterToVariable = new();

        private ContentPresenter? addActionPresenter;
        private NewActionViewModel? addActionViewModel;
        
        private ContentPresenter? addConditionPresenter;
        private NewConditionViewModel? addConditionViewModel;
        private bool draggingActions;
        private bool draggingEvents;
        private bool draggingConditions;
        private bool isCopying;

        private Point mouseStartPosition;
        private float mouseY;

        public (float y, float height, int eventIndex) OverIndexEvent { get; set; }
        private (float y, float height, int conditionIndex, int eventIndex) overIndexCondition;
        private (float y, float height, int actionIndex, int eventIndex) overIndexAction;

        private static double PaddingLeft = 20;
        
        private double EventWidth(double totalWidth) => Math.Min(Math.Max(totalWidth - 50, PaddingLeft + 10), 250);
        
        private double ConditionWidth(double totalWidth) => Math.Max(EventWidth(totalWidth) - 22 - PaddingLeft, 0);

        static SmartScriptPanelLayout()
        {
            AffectsRender<SmartScriptPanelLayout>(ProblemsProperty);
            PointerPressedEvent.AddClassHandler<SmartScriptPanelLayout>(PointerPressedHandled, RoutingStrategies.Tunnel, true);
        }

        private static void PointerPressedHandled(SmartScriptPanelLayout panel, PointerPressedEventArgs e)
        {
            panel.mouseStartPosition = e.GetPosition(panel);
        }
        
        protected override void LogicalChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.LogicalChildrenCollectionChanged(sender, e);
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var o in e.NewItems!)
                {
                    OnVisualChildrenChanged(o as ContentPresenter, null);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var o in e.OldItems!)
                {
                    OnVisualChildrenChanged(null, o as ContentPresenter);
                }
            }
            
            InvalidateArrange();
            InvalidateVisual();
            InvalidateMeasure();
        }

        protected void OnVisualChildrenChanged(ContentPresenter? visualAdded, ContentPresenter? visualRemoved)
        {
            if (visualAdded is ContentPresenter visualAddedPresenter)
            {
                visualAddedPresenter.DataContextChanged += OnLoadVisualChild;
                OnLoadVisualChild(visualAddedPresenter, EventArgs.Empty);
            }

            if (visualRemoved is ContentPresenter visualRemovedPresenter)
            {
                visualRemovedPresenter.DataContextChanged -= OnLoadVisualChild;
                if (visualRemovedPresenter.Content is SmartEvent @event)
                {
                    presenterToEvent.Remove(visualRemovedPresenter);
                    eventToPresenter.Remove(@event);
                }
                else if (visualRemovedPresenter.Content is SmartAction action)
                {
                    presenterToAction.Remove(visualRemovedPresenter);
                    actionToPresenter.Remove(action);
                }
                else if (visualRemovedPresenter.Content is SmartCondition condition)
                {
                    presenterToCondition.Remove(visualRemovedPresenter);
                    conditionToPresenter.Remove(condition);
                }
                else if (visualRemovedPresenter.Content is GlobalVariable globalVariable)
                {
                    presenterToVariable.Remove(visualRemovedPresenter);
                    variableToPresenter.Remove(globalVariable);
                }
                else if (visualRemovedPresenter.Content is NewActionViewModel)
                {
                    addActionPresenter = null;
                    addActionViewModel = null;
                }
                else if (visualRemovedPresenter.Content is NewConditionViewModel)
                {
                    addConditionPresenter = null;
                    addConditionViewModel = null;
                }
            }

            InvalidateArrange();
            InvalidateVisual();
            InvalidateMeasure();
        }

        private void OnLoadVisualChild(object? sender, EventArgs e)
        {
            if (sender is not ContentPresenter visualAddedPresenter)
                return;
            
            if (visualAddedPresenter.Content is SmartEvent @event)
            {
                presenterToEvent[visualAddedPresenter] = @event;
                eventToPresenter[@event] = visualAddedPresenter;
            }
            else if (visualAddedPresenter.Content is SmartAction action)
            {
                presenterToAction[visualAddedPresenter] = action;
                actionToPresenter[action] = visualAddedPresenter;
            }
            else if (visualAddedPresenter.Content is SmartCondition condition)
            {
                presenterToCondition[visualAddedPresenter] = condition;
                conditionToPresenter[condition] = visualAddedPresenter;
            }
            else if (visualAddedPresenter.Content is GlobalVariable globalVariable)
            {
                presenterToVariable[visualAddedPresenter] = globalVariable;
                variableToPresenter[globalVariable] = visualAddedPresenter;
            }
            else if (visualAddedPresenter.Content is NewActionViewModel vm)
            {
                addActionPresenter = visualAddedPresenter;
                addActionViewModel = vm;
            } else if (visualAddedPresenter.Content is NewConditionViewModel cvm)
            {
                addConditionPresenter = visualAddedPresenter;
                addConditionViewModel = cvm;
            }

            InvalidateArrange();
            InvalidateVisual();
            InvalidateMeasure();
        }

        private IEnumerable<ContentPresenter> Events()
        {
            foreach (ContentPresenter child in Children)
            {
                if (child.Content is SmartEvent)
                    yield return child;
            }
        }
        
        private IEnumerable<ContentPresenter> GlobalVariables()
        {
            foreach (ContentPresenter child in Children)
            {
                if (child.Content is GlobalVariable)
                    yield return child;
            }
        }

        private Size MeasureGlobalVariables(Size availableSize)
        {
            double totalDesiredSize = EventSpacing;
            foreach (ContentPresenter globalVariablePresenter in GlobalVariables())
            {
                globalVariablePresenter.Measure(new Size(availableSize.Width - PaddingLeft, availableSize.Height));
                totalDesiredSize += globalVariablePresenter.DesiredSize.Height + 2;
            }

            return new Size(availableSize.Width, totalDesiredSize);
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            float totalDesiredHeight = 0;
            double eventWidth = EventWidth((float) availableSize.Width);
            double actionWidth = availableSize.Width - eventWidth;
            double conditionWidth = ConditionWidth((float) availableSize.Width);

            totalDesiredHeight += (float)MeasureGlobalVariables(availableSize).Height;
            
            foreach (ContentPresenter eventPresenter in Events())
            {
                eventPresenter.Measure(new Size(eventWidth - PaddingLeft, availableSize.Height));

                float actionsHeight = 26;
                float conditionsHeight = 26;

                if (!presenterToEvent.ContainsKey(eventPresenter))
                    continue;

                SmartEvent @event = presenterToEvent[eventPresenter];
                foreach (SmartAction action in @event.Actions)
                {
                    if (!actionToPresenter.TryGetValue(action, out var actionPresenter))
                        continue;

                    actionPresenter.Measure(new Size(actionWidth, availableSize.Height));

                    actionsHeight += (float) actionPresenter.DesiredSize.Height + ActionSpacing;
                }
                foreach (SmartCondition condition in @event.Conditions)
                {
                    if (!conditionToPresenter.TryGetValue(condition, out var conditionPresenter))
                        continue;

                    conditionPresenter.Measure(new Size(conditionWidth, availableSize.Height));

                    conditionsHeight += (float) conditionPresenter.DesiredSize.Height + ConditionSpacing;
                }

                totalDesiredHeight += Math.Max(actionsHeight, (float) eventPresenter.DesiredSize.Height + conditionsHeight) + EventSpacing;
            }

            return new Size(availableSize.Width, Math.Max(0, totalDesiredHeight));
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            UpdateIsCopying(e.KeyModifiers);
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
                e.ClickCount == 1)
            {
                foreach (ContentPresenter @event in Children)
                    if (@event.Child != null)
                        SetSelected(@event.Child, false);
            }            
        }

        private void UpdateIsCopying(KeyModifiers key)
        {
            var systemWideControlModifier = AvaloniaLocator.Current
                .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
            isCopying = key.HasFlagFast(systemWideControlModifier);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            var systemWideControlModifier = AvaloniaLocator.Current
                .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
            
            base.OnPointerReleased(e);
            UpdateIsCopying(e.KeyModifiers);
            StopDragging();
        }

        private bool AnythingSelected()
        {
            foreach (var e in Script.Events)
            {
                if (e.IsSelected)
                    return true;
            }
            foreach (var e in Script.Events)
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
                if (draggingEvents)
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
            InvalidateArrange();
            InvalidateVisual();
        }

        private static FormattedText? vvvvText;
        private static FormattedTextNumberCache NumberCache = new();
        public override void Render(DrawingContext dc)
        {
            base.Render(dc);
            if (AnythingSelected())
            {
                if (draggingActions)
                {
                    double x = EventWidth(Bounds.Width);
                    float y = overIndexAction.y - overIndexAction.height / 2 - 1;
                    dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(x, y), new Point(x + 200, y));
                }
                else if (draggingConditions)
                {
                    float x = 1;
                    float y = overIndexCondition.y - overIndexCondition.height / 2 - 1;
                    dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(x, y), new Point(x + ConditionWidth(Bounds.Width), y));         
                }
                else if (draggingEvents)
                {
                    if (isCopying)
                    {
                        float x = 1;
                        float y = OverIndexEvent.y - OverIndexEvent.height / 2 - 1;
                        dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(x, y), new Point(x + EventWidth(Bounds.Width), y));   
                    }
                }
            }

            if (vvvvText == null)
            {
                vvvvText = new FormattedText();
                vvvvText.FontSize = 7;
                vvvvText.Text = "vvvv";
                vvvvText.Typeface = Typeface.Default;
            }
            
            int index = 1;
            double yPos = 0;
            foreach (var e in Script.Events)
            {
                if (e.Actions.Count == 0)
                {
                    if (!eventToPresenter.TryGetValue(e, out var eventPresenter))
                        continue;
                    yPos = eventPresenter.Bounds.Y;

                    var ft = NumberCache.Get(index);
                    dc.DrawText(Brushes.DarkGray, new Point(0, yPos + 5), ft);
                    DrawProblems(dc, index, yPos);
                    index++;
                }
                else
                {
                    foreach (var a in e.Actions)
                    {
                        if (!actionToPresenter.TryGetValue(a, out var actionPresenter))
                            continue;
                        
                        yPos = actionPresenter.Bounds.Y;

                        var ft = NumberCache.Get(index);
                        dc.DrawText(Brushes.DarkGray, new Point(0, yPos + 5), ft);
                        DrawProblems(dc, index, yPos);
                        index++;
                    }
                }
            }
        }

        private void DrawProblems(DrawingContext dc, int index, double yPos)
        {
            if (Problems != null && Problems.TryGetValue(index, out var severity))
            { 
                dc.DrawText(severity == DiagnosticSeverity.Error ? Brushes.Red : Brushes.Orange, new Point(0, yPos + 5 + 10), vvvvText);   
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
                    cache[i] = new FormattedText();
                    cache[i].Text = $"{i}";
                    cache[i].FontSize = 10;
                    cache[i].Typeface = Typeface.Default;
                }
            }
        }

        private bool AnyActionSelected()
        {
            return actionToPresenter.Keys.Any(a => a.IsSelected);
        }

        private bool AnyEventSelected()
        {
            return eventToPresenter.Keys.Any(a => a.IsSelected);
        }

        private bool mouseStartPositionValid = false;

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            UpdateIsCopying(e.KeyModifiers);
            var state = e.GetCurrentPoint(this);

            mouseY = (float) e.GetPosition(this).Y;
            if (!state.Properties.IsLeftButtonPressed)
            {
                mouseStartPosition = e.GetPosition(this);
                mouseStartPositionValid = true;
            }
            if (mouseStartPositionValid && 
                state.Properties.IsLeftButtonPressed &&
                !draggingActions && !draggingEvents && !draggingConditions)
            {
                var d = new Point(mouseStartPosition.X - e.GetPosition(this).X,
                    mouseStartPosition.Y - e.GetPosition(this).Y);
                var dist = d.X * d.X + d.Y * d.Y;
                if (dist > 10 * 10)
                {
                    if (e.GetPosition(this).X < EventWidth(Bounds.Width))
                    {
                        if (AnyEventSelected())
                            draggingEvents = true;
                        else
                            draggingConditions = true;
                    }
                    else
                        draggingActions = AnyActionSelected();
            
                    if (draggingEvents || draggingActions || draggingConditions)
                    {
                        mouseStartPositionValid = false;
                        //CaptureMouse();
                    }
                }
            }
            
            if (draggingEvents)
            {
                var eventIndex = 0;
                var found = false;
                foreach (var tuple in eventHeights)
                {
                    if (tuple.y > mouseY)
                    {
                        OverIndexEvent = tuple;
                        found = true;
                        break;
                    }
            
                    eventIndex++;
                }
            
                if (!found && eventHeights.Count >= 1)
                {
                    OverIndexEvent = eventHeights[^1];//eventHeights.Count > 0 && mouseY > eventHeights[^1].y
                        //? eventHeights[^1]
                        //: eventHeights[0];
                }
            }
            else if (draggingActions)
            {
                foreach ((float y, float height, int actionIndex, int eventIndex) tuple in actionHeights)
                {
                    if (tuple.y > mouseY)
                    {
                        overIndexAction = tuple;
                        break;
                    }
                }
                InvalidateVisual();
            }
            else if (draggingConditions)
            {
                foreach ((float y, float height, int conditionIndex, int eventIndex) tuple in conditionHeights)
                {
                    if (tuple.y > mouseY)
                    {
                        overIndexCondition = tuple;
                        break;
                    }
                }
                InvalidateVisual();
            }

            InvalidateArrange();
        }

        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);
            mouseStartPositionValid = false;
        }

        private Size ArrangeGlobalVariables(Size finalSize)
        {
            double totalHeight = EventSpacing;
            foreach (GlobalVariable gv in Script.GlobalVariables)
            {
                if (!variableToPresenter.TryGetValue(gv, out var variablePresenter))
                    continue;
                
                variablePresenter.Arrange(new Rect(PaddingLeft, totalHeight, finalSize.Width - PaddingLeft, variablePresenter.DesiredSize.Height));

                totalHeight += variablePresenter.DesiredSize.Height + 2;
            }

            return new Size(finalSize.Width, totalHeight);
        }
        
        protected override Size ArrangeOverride(Size finalSize)
        {
            eventHeights.Clear();
            actionHeights.Clear();
            conditionHeights.Clear();
            Size globalVariablesArrangement = ArrangeGlobalVariables(finalSize);
            float y = (float)globalVariablesArrangement.Height;

            if (draggingActions || draggingConditions || draggingEvents)
            {
                addActionPresenter?.Arrange(new Rect(-5, -5, 1, 1));
                addConditionPresenter?.Arrange(new Rect(-5, -5, 1, 1));
            }
            
            float lastHeight = 0;
            float selectedHeight = 0;
            int eventIndex = 0;
            foreach (SmartEvent ev in Script.Events)
            {
                eventIndex++;
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                
                lastHeight = Math.Max(MeasureActions(eventPresenter), (float) eventPresenter.DesiredSize.Height + MeasureConditions(eventPresenter));
                eventHeights.Add((y + (lastHeight + EventSpacing) / 2, lastHeight + EventSpacing, eventIndex - 1));
                y += lastHeight + EventSpacing;

                if (!draggingEvents || isCopying || !GetSelected(eventPresenter.Child))
                    continue;
                selectedHeight += lastHeight + EventSpacing;
            }

            eventHeights.Add((y, 0, eventIndex));

            y = (float)globalVariablesArrangement.Height;
            float start = 0;
            if (OverIndexEvent.eventIndex == 0)
            {
                start = y;
                y += selectedHeight;
            }

            eventIndex = 0;
            if (addActionViewModel != null)
                addActionViewModel.Event = null;
            if (addConditionViewModel != null)
                addConditionViewModel.Event = null;
            
            foreach (SmartEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;

                var height = (float)eventPresenter.DesiredSize.Height;
                if (!draggingEvents || isCopying || !GetSelected(eventPresenter.Child))
                {
                    float actionHeight = ArrangeActions(eventIndex, 0, finalSize, eventPresenter, y, height);
                    float conditionsHeight = ArrangeConditions(eventIndex, (float)PaddingLeft, finalSize, eventPresenter, y, height);
                    float eventsConditionsHeight = height + conditionsHeight;
                    height = Math.Max(eventsConditionsHeight, actionHeight);
                    eventPresenter.Arrange(new Rect(PaddingLeft, y, EventWidth(finalSize.Width) - PaddingLeft, height));

                    if (mouseY > y && mouseY < y + height && !draggingActions && !draggingEvents)
                    {
                        if (presenterToEvent.TryGetValue(eventPresenter, out SmartEvent? smartEvent) && smartEvent != null)
                        {
                            if (addActionViewModel != null)
                                addActionViewModel.Event = smartEvent;
                                
                            if (addConditionViewModel != null)
                                addConditionViewModel.Event = smartEvent;                            
                        }
                            
                        addActionPresenter?.Arrange(new Rect(EventWidth(finalSize.Width),
                            y + actionHeight - 26,
                            Math.Max(finalSize.Width - EventWidth(finalSize.Width), 0),
                            24));
                            
                        addConditionPresenter?.Arrange(new Rect(PaddingLeft + 25,
                            y + eventsConditionsHeight - 26,
                            ConditionWidth(finalSize.Width),
                            24));
                    }

                    y += height + EventSpacing;
                }

                eventIndex++;
                if (eventIndex == OverIndexEvent.eventIndex)
                {
                    start = y;
                    y += selectedHeight;
                }
            }

            eventIndex = 0;
            foreach (SmartEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                var height = (float) eventPresenter.DesiredSize.Height;
                if (draggingEvents && !isCopying && GetSelected(eventPresenter.Child))
                {
                    float conditionsHeight = ArrangeConditions(eventIndex, 20 + (float)PaddingLeft, finalSize, eventPresenter, start, height);
                    height = Math.Max(height + conditionsHeight, ArrangeActions(eventIndex, 20, finalSize, eventPresenter, start, height));
                    eventPresenter.Arrange(new Rect(20 + PaddingLeft, start, EventWidth(finalSize.Width) - PaddingLeft, height));
                    start += height + EventSpacing;
                }

                eventIndex++;
            }
            return finalSize;
        }

        private float ArrangeActions(int eventIndex,
            float x,
            Size totalSize,
            ContentPresenter eveentPresenter,
            float y,
            float eventHeight)
        {
            float totalHeight = 26;
            if (!presenterToEvent.TryGetValue(eveentPresenter, out SmartEvent? @event))
                return totalHeight;

            var actionIndex = 0;
            foreach (SmartAction action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out ContentPresenter? actionPresenter))
                    continue;

                var height = (float) actionPresenter.DesiredSize.Height;
                actionPresenter.Arrange(new Rect(EventWidth(totalSize.Width) + x, y, Math.Max(totalSize.Width - EventWidth(totalSize.Width), 0), height));
                actionHeights.Add((y + (height + ActionSpacing) / 2, height + ActionSpacing, actionIndex, eventIndex));
                y += height + ActionSpacing;

                totalHeight += height + ActionSpacing;
                actionIndex++;
            }

            float rest = Math.Max(26, eventHeight - (totalHeight - 26));

            actionHeights.Add((y + (rest + ActionSpacing) / 2, rest, actionIndex, eventIndex));

            return totalHeight;
        }

        private float MeasureActions(ContentPresenter eventPresenter)
        {
            float totalHeight = 26;
            if (!presenterToEvent.TryGetValue(eventPresenter, out SmartEvent? @event))
                return totalHeight;

            foreach (SmartAction action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out ContentPresenter? actionPresenter))
                    continue;

                var height = (float) actionPresenter.DesiredSize.Height;
                totalHeight += height + ActionSpacing;
            }

            return totalHeight;
        }
        
        private float MeasureConditions(ContentPresenter eventPresenter)
        {
            float totalHeight = 26;
            if (!presenterToEvent.TryGetValue(eventPresenter, out SmartEvent? @event))
                return totalHeight;

            foreach (SmartCondition condition in @event.Conditions)
            {
                if (!conditionToPresenter.TryGetValue(condition, out ContentPresenter? actionPresenter))
                    continue;

                var height = (float) actionPresenter.DesiredSize.Height;
                totalHeight += height + ConditionSpacing;
            }

            return totalHeight;
        }
        
        private float ArrangeConditions(int eventIndex,
            float x,
            Size totalSize,
            ContentPresenter eveentPresenter,
            float y,
            float eventHeight)
        {
            float totalHeight = 26;
            if (!presenterToEvent.TryGetValue(eveentPresenter, out SmartEvent? @event))
                return totalHeight;

            var conditionIndex = 0;
            y += eventHeight;
            foreach (SmartCondition condition in @event.Conditions)
            {
                if (!conditionToPresenter.TryGetValue(condition, out ContentPresenter? conditionPresenter))
                    continue;

                var height = (float) conditionPresenter.DesiredSize.Height;
                conditionPresenter.Arrange(new Rect(x + 21, y, ConditionWidth(totalSize.Width), height));
                conditionHeights.Add((y + (height + ConditionSpacing) / 2, height + ConditionSpacing, conditionIndex, eventIndex));
                y += height + ConditionSpacing;

                totalHeight += height + ConditionSpacing;
                conditionIndex++;
            }

            float rest = 5;

            conditionHeights.Add((y + (rest + ActionSpacing) / 2, rest, conditionIndex, eventIndex));

            return totalHeight;
        }

        // protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        // {
        //     base.OnRenderSizeChanged(sizeInfo);
        //     InvalidateMeasure();
        //     InvalidateArrange();
        // }
        //

        public SmartScript Script
        {
            get => (SmartScript) GetValue(ScriptProperty);
            set => SetValue(ScriptProperty, value);
        }

        public static readonly AvaloniaProperty ScriptProperty =
            AvaloniaProperty.Register<SmartScriptPanelLayout, SmartScript?>(nameof(Script), null);
        
        public Dictionary<int, DiagnosticSeverity>? Problems
        {
            get => (Dictionary<int, DiagnosticSeverity>?) GetValue(ProblemsProperty);
            set => SetValue(ProblemsProperty, value);
        }
        public static readonly AvaloniaProperty ProblemsProperty =
            AvaloniaProperty.Register<SmartScriptPanelLayout, Dictionary<int, DiagnosticSeverity>?>(nameof(Problems));

        public float EventSpacing => 10;

        public float ConditionSpacing => 2;
        public float ActionSpacing => 2;

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<SmartScriptPanelLayout, IControl, bool>("Selected");
        public static bool GetSelected(IControl control) => (bool)control.GetValue(SelectedProperty);
        public static void SetSelected(IControl control, bool value) => control.SetValue(SelectedProperty, value);

        public static readonly AvaloniaProperty DropItemsProperty = AvaloniaProperty.Register<SmartScriptPanelLayout, ICommand>(nameof(DropItems));

        public ICommand DropItems
        {
            get => (ICommand) GetValue(DropItemsProperty);
            set => SetValue(DropItemsProperty, value);
        }

        public static readonly AvaloniaProperty DropActionsProperty = AvaloniaProperty.Register<SmartScriptPanelLayout, ICommand>(nameof(DropActions));

        public ICommand DropActions
        {
            get => (ICommand) GetValue(DropActionsProperty);
            set => SetValue(DropActionsProperty, value);
        }

        public static readonly AvaloniaProperty DropConditionsProperty = AvaloniaProperty.Register<SmartScriptPanelLayout, ICommand>(nameof(DropConditions));

        public ICommand DropConditions
        {
            get => (ICommand) GetValue(DropConditionsProperty);
            set => SetValue(DropConditionsProperty, value);
        }
    }
}