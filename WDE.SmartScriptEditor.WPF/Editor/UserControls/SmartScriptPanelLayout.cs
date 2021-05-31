using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.WPF.Editor.UserControls
{
    internal class SmartScriptPanelLayout : Panel
    {
        private readonly List<(float y, float height, int actionIndex, int eventIndex)> actionHeights = new();
        private readonly List<(float y, float height, int conditionIndex, int eventIndex)> conditionHeights = new();

        private readonly List<float> eventHeights = new();
        private readonly Dictionary<SmartAction, ContentPresenter> actionToPresenter = new();
        private readonly Dictionary<SmartEvent, ContentPresenter> eventToPresenter = new();
        private readonly Dictionary<SmartCondition, ContentPresenter> conditionToPresenter = new();
        private readonly Dictionary<ContentPresenter, SmartCondition> presenterToCondition = new();
        private readonly Dictionary<ContentPresenter, SmartAction> presenterToAction = new();
        private readonly Dictionary<ContentPresenter, SmartEvent> presenterToEvent = new();
        private readonly Dictionary<GlobalVariable, ContentPresenter> variableToPresenter = new();
        private readonly Dictionary<ContentPresenter, GlobalVariable> presenterToVariable = new();

        private ContentPresenter addActionPresenter;
        private NewActionViewModel addActionViewModel;
        
        private ContentPresenter addConditionPresenter;
        private NewConditionViewModel addConditionViewModel;
        private bool draggingActions;
        private bool draggingEvents;
        private bool draggingConditions;

        private Point mouseStartPosition;
        private float mouseY;

        private (float y, float height, int conditionIndex, int eventIndex) overIndexCondition;
        private (float y, float height, int actionIndex, int eventIndex) overIndexAction;

        private static float PaddingLeft = 20;
        private float EventWidth => Math.Min(Math.Max((float) ActualWidth - 50, PaddingLeft + 10), 250);

        private float ConditionWidth => Math.Max(EventWidth - 22 - PaddingLeft, 0);

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            if (visualAdded is ContentPresenter visualAddedPresenter)
                visualAddedPresenter.Loaded += OnLoadVisualChild;

            if (visualRemoved is ContentPresenter visualRemovedPresenter)
            {
                visualRemovedPresenter.Loaded -= OnLoadVisualChild;
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

        private void OnLoadVisualChild(object sender, RoutedEventArgs e)
        {
            ContentPresenter visualAddedPresenter = sender as ContentPresenter;
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
            foreach (ContentPresenter child in InternalChildren)
            {
                if (child.Content is SmartEvent)
                    yield return child;
            }
        }

        private IEnumerable<ContentPresenter> GlobalVariables()
        {
            foreach (ContentPresenter child in InternalChildren)
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
                globalVariablePresenter.Measure(new Size(ActualWidth - PaddingLeft, availableSize.Height));
                totalDesiredSize += globalVariablePresenter.DesiredSize.Height + 2;
            }

            return new Size(availableSize.Width, totalDesiredSize);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size s = base.MeasureOverride(availableSize);
            float totalDesiredHeight = (float)MeasureGlobalVariables(availableSize).Height;
            foreach (ContentPresenter eventPresenter in Events())
            {
                eventPresenter.Measure(new Size(EventWidth - PaddingLeft, availableSize.Height));

                float actionsHeight = 26;
                float conditionsHeight = 26;

                if (!presenterToEvent.ContainsKey(eventPresenter))
                    continue;

                SmartEvent @event = presenterToEvent[eventPresenter];
                foreach (SmartAction action in @event.Actions)
                {
                    if (!actionToPresenter.TryGetValue(action, out var actionPresenter))
                        continue;

                    actionPresenter.Measure(new Size(ActualWidth - EventWidth, availableSize.Height));

                    actionsHeight += (float) actionPresenter.DesiredSize.Height + ActionSpacing;
                }
                foreach (SmartCondition condition in @event.Conditions)
                {
                    if (!conditionToPresenter.TryGetValue(condition, out var conditionPresenter))
                        continue;

                    conditionPresenter.Measure(new Size(ConditionWidth, availableSize.Height));

                    conditionsHeight += (float) conditionPresenter.DesiredSize.Height + ConditionSpacing;
                }

                totalDesiredHeight += Math.Max(actionsHeight, (float) eventPresenter.DesiredSize.Height + conditionsHeight) + EventSpacing;
            }

            return new Size(s.Width, Math.Max(0, totalDesiredHeight));
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!(e.Source is SmartEventView) && !(e.Source is SmartActionView))
                {
                    foreach (ContentPresenter @event in Events())
                        SetSelected(@event, false);
                }

                mouseStartPosition = e.GetPosition(this);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            StopDragging();
        }

        private void StopDragging()
        {
            ReleaseMouseCapture();
            if (draggingEvents)
                DropItems?.Execute(OverIndexEvent);
            else if (draggingActions)
            {
                DropActions?.Execute(new DropActionsConditionsArgs
                    {EventIndex = overIndexAction.eventIndex, ActionIndex = overIndexAction.actionIndex});
            }
            else if (draggingConditions)
            {
                DropConditions?.Execute(new DropActionsConditionsArgs
                    {EventIndex = overIndexCondition.eventIndex, ActionIndex = overIndexCondition.conditionIndex});
            }

            draggingEvents = false;
            draggingActions = false;
            draggingConditions = false;
            InvalidateArrange();
            InvalidateVisual();
        }

        private static FormattedText? vvvvError;
        private static FormattedText? vvvvWarning;

        private void DrawProblem(DrawingContext dc, int line, double yPos)
        {
            if (vvvvError == null)
            {
                vvvvError = new FormattedText("vvvv", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Calibri"), 7, Brushes.Red);
                vvvvWarning = new FormattedText("vvvv", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Calibri"), 7, Brushes.Orange);
            }

            if (Problems != null && Problems.TryGetValue(line, out var severity))
            {
                dc.DrawText(severity == DiagnosticSeverity.Error ? vvvvError : vvvvWarning, new Point(0, yPos + 5 + 10));
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (draggingActions)
            {
                float x = EventWidth;
                float y = overIndexAction.y - overIndexAction.height / 2 - 1;
                dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(x, y), new Point(x + 200, y));
            }
            else if (draggingConditions)
            {
                float x = 1;
                float y = overIndexCondition.y - overIndexCondition.height / 2 - 1;
                dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(x, y), new Point(x + ConditionWidth, y));         
            }


            int index = 1;
            double yPos = 0;
            foreach (var e in Script.Events)
            {
                if (e.Actions.Count == 0)
                {
                    if (!eventToPresenter.TryGetValue(e, out var eventPresenter))
                        continue;
                    yPos = eventPresenter.TransformToAncestor(this).Transform(new Point(0, 0)).Y;

                    var ft = NumberCache.Get(index);
                    dc.DrawText(ft, new Point(0, yPos + 5));
                    DrawProblem(dc, index, yPos);
                    index++;
                }
                else
                {
                    foreach (var a in e.Actions)
                    {
                        if (!actionToPresenter.TryGetValue(a, out var actionPresenter))
                            continue;

                        yPos = actionPresenter.TransformToAncestor(this).Transform(new Point(0, 0)).Y;

                        var ft = NumberCache.Get(index);
                        dc.DrawText(ft, new Point(0, yPos + 5));
                        DrawProblem(dc, index, yPos);

                        index++;
                    }
                }
            }
        }


        private static FormattedTextNumberCache NumberCache = new();
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
                    cache[i] = new FormattedText(i.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Calibri"), 10, Brushes.DarkGray);
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
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseY = (float) e.GetPosition(this).Y;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                mouseStartPosition = e.GetPosition(this);
                mouseStartPositionValid = true;
            }
            if (mouseStartPositionValid && 
                e.LeftButton == MouseButtonState.Pressed &&
                !draggingActions && !draggingEvents && !draggingConditions)
            {
                var dist = (float) Point.Subtract(mouseStartPosition, e.GetPosition(this)).Length;
                if (dist > 10)
                {
                    if (e.GetPosition(this).X < EventWidth)
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
                        CaptureMouse();
                    }
                }
            }

            if (draggingEvents)
            {
                var eventIndex = 0;
                var found = false;
                foreach (float f in eventHeights)
                {
                    if (f > mouseY)
                    {
                        OverIndexEvent = eventIndex;
                        found = true;
                        break;
                    }

                    eventIndex++;
                }

                if (!found)
                {
                    OverIndexEvent = eventHeights.Count > 0 && mouseY > eventHeights[^1]
                        ? eventHeights.Count - 1
                        : 0;
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

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
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

            float selectedHeight = 0;
            foreach (SmartEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                
                float height = Math.Max(MeasureActions(eventPresenter), (float) eventPresenter.DesiredSize.Height + MeasureConditions(eventPresenter));
                eventHeights.Add(y + (height + EventSpacing) / 2);
                y += height + EventSpacing;

                if (!draggingEvents || !GetSelected(eventPresenter))
                    continue;
                selectedHeight += height + EventSpacing;
            }

            eventHeights.Add(y);

            y = (float)globalVariablesArrangement.Height;
            float start = 0;
            if (OverIndexEvent == 0)
            {
                start = y;
                y += selectedHeight;
            }

            var eventIndex = 0;
            if (addActionViewModel != null)
                addActionViewModel.Event = null;
            if (addConditionViewModel != null)
                addConditionViewModel.Event = null;
            
            foreach (SmartEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                
                var height = (float) eventPresenter.DesiredSize.Height;
                if (!draggingEvents || !GetSelected(eventPresenter))
                {
                    float actionHeight = ArrangeActions(eventIndex, 0, finalSize, eventPresenter, y, height);
                    float conditionsHeight = ArrangeConditions(eventIndex, PaddingLeft, finalSize, eventPresenter, y, height);
                    float eventsConditionsHeight = height + conditionsHeight;
                    height = Math.Max(eventsConditionsHeight, actionHeight);
                    eventPresenter.Arrange(new Rect(PaddingLeft, y, EventWidth - PaddingLeft, height));

                    if (mouseY > y && mouseY < y + height && !draggingActions && !draggingEvents)
                    {
                        if (presenterToEvent.TryGetValue(eventPresenter, out SmartEvent smartEvent))
                        {
                            if (addActionViewModel != null)
                                addActionViewModel.Event = smartEvent;
                                
                            if (addConditionViewModel != null)
                                addConditionViewModel.Event = smartEvent;                            
                        }
                            
                        addActionPresenter.Arrange(new Rect(EventWidth,
                            y + actionHeight - 26,
                            Math.Max(finalSize.Width - EventWidth, 0),
                            24));
                            
                        addConditionPresenter.Arrange(new Rect(PaddingLeft + 25,
                            y + eventsConditionsHeight - 26,
                            ConditionWidth,
                            24));
                    }

                    y += height + EventSpacing;
                }

                eventIndex++;
                if (eventIndex == OverIndexEvent)
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
                if (draggingEvents && GetSelected(eventPresenter))
                {
                    float conditionsHeight = ArrangeConditions(eventIndex, 20 + PaddingLeft, finalSize, eventPresenter, start, height);
                    height = Math.Max(height + conditionsHeight, ArrangeActions(eventIndex, 20, finalSize, eventPresenter, start, height));
                    eventPresenter.Arrange(new Rect(20 + PaddingLeft, start, EventWidth - PaddingLeft, height));
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
            if (!presenterToEvent.TryGetValue(eveentPresenter, out SmartEvent @event))
                return totalHeight;

            var actionIndex = 0;
            foreach (SmartAction action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out ContentPresenter actionPresenter))
                    continue;

                var height = (float) actionPresenter.DesiredSize.Height;
                actionPresenter.Arrange(new Rect(EventWidth + x, y, Math.Max(totalSize.Width - EventWidth, 0), height));
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
            if (!presenterToEvent.TryGetValue(eventPresenter, out SmartEvent @event))
                return totalHeight;

            foreach (SmartAction action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out ContentPresenter actionPresenter))
                    continue;

                var height = (float) actionPresenter.DesiredSize.Height;
                totalHeight += height + ActionSpacing;
            }

            return totalHeight;
        }
        
        private float MeasureConditions(ContentPresenter eventPresenter)
        {
            float totalHeight = 26;
            if (!presenterToEvent.TryGetValue(eventPresenter, out SmartEvent @event))
                return totalHeight;

            foreach (SmartCondition condition in @event.Conditions)
            {
                if (!conditionToPresenter.TryGetValue(condition, out ContentPresenter actionPresenter))
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
            if (!presenterToEvent.TryGetValue(eveentPresenter, out SmartEvent @event))
                return totalHeight;

            var conditionIndex = 0;
            y += eventHeight;
            foreach (SmartCondition condition in @event.Conditions)
            {
                if (!conditionToPresenter.TryGetValue(condition, out ContentPresenter conditionPresenter))
                    continue;

                var height = (float) conditionPresenter.DesiredSize.Height;
                conditionPresenter.Arrange(new Rect(x + 21, y, ConditionWidth, height));
                conditionHeights.Add((y + (height + ConditionSpacing) / 2, height + ConditionSpacing, conditionIndex, eventIndex));
                y += height + ConditionSpacing;

                totalHeight += height + ConditionSpacing;
                conditionIndex++;
            }

            float rest = 5;

            conditionHeights.Add((y + (rest + ActionSpacing) / 2, rest, conditionIndex, eventIndex));

            return totalHeight;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateMeasure();
            InvalidateArrange();
        }

        #region Properties

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.RegisterAttached("Selected",
            typeof(bool),
            typeof(SmartScriptPanelLayout),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        [AttachedPropertyBrowsableForChildren]
        public static bool GetSelected(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (bool) element.GetValue(SelectedProperty);
        }

        [AttachedPropertyBrowsableForChildren]
        public static void SetSelected(UIElement element, bool length)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(SelectedProperty, length);
        }

        public int OverIndexEvent
        {
            get => (int) GetValue(OverIndexEventProperty);
            set => SetValue(OverIndexEventProperty, value);
        }

        public static readonly DependencyProperty OverIndexEventProperty = DependencyProperty.Register(nameof(OverIndexEvent),
            typeof(int),
            typeof(SmartScriptPanelLayout),
            new PropertyMetadata(0));

        
        public SmartScript Script
        {
            get => (SmartScript) GetValue(ScriptProperty);
            set => SetValue(ScriptProperty, value);
        }

        public static readonly DependencyProperty ScriptProperty = DependencyProperty.Register(nameof(Script),
            typeof(SmartScript),
            typeof(SmartScriptPanelLayout),
            new PropertyMetadata(null));

        public Dictionary<int, DiagnosticSeverity>? Problems
        {
            get => (Dictionary<int, DiagnosticSeverity>?)GetValue(ProblemsProperty);
            set => SetValue(ProblemsProperty, value);
        }

        public static readonly DependencyProperty ProblemsProperty = DependencyProperty.Register(nameof(Problems),
            typeof(Dictionary<int, DiagnosticSeverity>),
            typeof(SmartScriptPanelLayout),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public float EventSpacing
        {
            get => (float) GetValue(EventSpacingProperty);
            set => SetValue(EventSpacingProperty, value);
        }

        public static readonly DependencyProperty EventSpacingProperty = DependencyProperty.Register(nameof(EventSpacing),
            typeof(float),
            typeof(SmartScriptPanelLayout),
            new PropertyMetadata(10f));
            
        public float ConditionSpacing
        {
            get => (float) GetValue(ConditionSpacingProperty);
            set => SetValue(ConditionSpacingProperty, value);
        }

        public static readonly DependencyProperty ConditionSpacingProperty = DependencyProperty.Register(nameof(ConditionSpacing),
            typeof(float),
            typeof(SmartScriptPanelLayout),
            new PropertyMetadata(1f));
            
        public float ActionSpacing
        {
            get => (float) GetValue(ActionSpacingProperty);
            set => SetValue(ActionSpacingProperty, value);
        }

        public static readonly DependencyProperty ActionSpacingProperty = DependencyProperty.Register(nameof(ActionSpacing),
            typeof(float),
            typeof(SmartScriptPanelLayout),
            new PropertyMetadata(2f));

        public static readonly DependencyProperty DropItemsProperty = DependencyProperty.Register(nameof(DropItems),
            typeof(ICommand),
            typeof(SmartScriptPanelLayout),
            new UIPropertyMetadata(null));

        public ICommand DropItems
        {
            get => (ICommand) GetValue(DropItemsProperty);
            set => SetValue(DropItemsProperty, value);
        }

        public static readonly DependencyProperty DropActionsProperty = DependencyProperty.Register(nameof(DropActions),
            typeof(ICommand),
            typeof(SmartScriptPanelLayout),
            new UIPropertyMetadata(null));

        public ICommand DropActions
        {
            get => (ICommand) GetValue(DropActionsProperty);
            set => SetValue(DropActionsProperty, value);
        }

        public static readonly DependencyProperty DropConditionsProperty = DependencyProperty.Register(nameof(DropConditions),
            typeof(ICommand),
            typeof(SmartScriptPanelLayout),
            new UIPropertyMetadata(null));

        public ICommand DropConditions
        {
            get => (ICommand) GetValue(DropConditionsProperty);
            set => SetValue(DropConditionsProperty, value);
        }
        
        #endregion
    }
}