using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
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

        private double EventWidth(double totalWidth) => Math.Min(Math.Max(totalWidth - 50, 0), 250);
        
        private double ConditionWidth(double totalWidth) => Math.Max(EventWidth(totalWidth) - 22, 0);

        protected override void LogicalChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.LogicalChildrenCollectionChanged(sender, e);
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var o in e.NewItems)
                {
                    OnVisualChildrenChanged(o as ContentPresenter, null);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                
                foreach (var o in e.OldItems)
                {
                    OnVisualChildrenChanged(null, o as ContentPresenter);
                }
            }
            
            InvalidateArrange();
            InvalidateVisual();
            InvalidateMeasure();
        }

        protected void OnVisualChildrenChanged(ContentPresenter visualAdded, ContentPresenter visualRemoved)
        {
            if (visualAdded is ContentPresenter visualAddedPresenter)
            {
                visualAddedPresenter.DataContextChanged += OnLoadVisualChild;
                OnLoadVisualChild(visualAddedPresenter, null);
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

        private void OnLoadVisualChild(object sender, EventArgs e)
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

        protected override Size MeasureOverride(Size availableSize)
        {
            Console.WriteLine("measuring");
            //Size s = base.MeasureOverride(availableSize);
            float totalDesiredHeight = EventSpacing;
            double eventWidth = EventWidth((float) availableSize.Width);
            double actionWidth = availableSize.Width - eventWidth;
            double conditionWidth = ConditionWidth((float) availableSize.Width);
            
            foreach (ContentPresenter eventPresenter in Events())
            {
                eventPresenter.Measure(new Size(eventWidth, availableSize.Height));

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

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var logic = ((ILogical) e.Source);
                if (!(e.Source.InteractiveParent.InteractiveParent.InteractiveParent is SmartEventView) && !(e.Source.InteractiveParent.InteractiveParent.InteractiveParent is SmartActionView))
                {
                    foreach (ContentPresenter @event in Events())
                        SetSelected(@event.Child, false);
                }
                else if (e.Source.InteractiveParent.InteractiveParent.InteractiveParent is SmartEventView ve)
                    SetSelected(ve, true);
        
                mouseStartPosition = e.GetPosition(this);
            }            
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            StopDragging();
        }

        private void StopDragging()
        {
//            ReleaseMouseCapture();
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
        
        public override void Render(DrawingContext dc)
        {
            base.Render(dc);
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

        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);
            mouseStartPositionValid = false;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Console.WriteLine("arranging");
            eventHeights.Clear();
            actionHeights.Clear();
            conditionHeights.Clear();
            float y = EventSpacing;

            float selectedHeight = 0;
            foreach (SmartEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                
                float height = Math.Max(MeasureActions(eventPresenter), (float) eventPresenter.DesiredSize.Height + MeasureConditions(eventPresenter));
                eventHeights.Add(y + (height + EventSpacing) / 2);
                y += height + EventSpacing;

                if (!draggingEvents || !GetSelected(eventPresenter.Child))
                    continue;
                selectedHeight += height + EventSpacing;
            }

            eventHeights.Add(y);

            y = EventSpacing;
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

                var height = (float)eventPresenter.DesiredSize.Height;
                if (!draggingEvents || !GetSelected(eventPresenter.Child))
                {
                    float actionHeight = ArrangeActions(eventIndex, 0, finalSize, eventPresenter, y, height);
                    float conditionsHeight = ArrangeConditions(eventIndex, 0, finalSize, eventPresenter, y, height);
                    float eventsConditionsHeight = height + conditionsHeight;
                    height = Math.Max(eventsConditionsHeight, actionHeight);
                    eventPresenter.Arrange(new Rect(0, y, EventWidth(finalSize.Width), height));

                    if (mouseY > y && mouseY < y + height && !draggingActions && !draggingEvents)
                    {
                        if (presenterToEvent.TryGetValue(eventPresenter, out SmartEvent smartEvent))
                        {
                            if (addActionViewModel != null)
                                addActionViewModel.Event = smartEvent;
                                
                            if (addConditionViewModel != null)
                                addConditionViewModel.Event = smartEvent;                            
                        }
                            
                        addActionPresenter.Arrange(new Rect(EventWidth(finalSize.Width),
                            y + actionHeight - 26,
                            Math.Max(finalSize.Width - EventWidth(finalSize.Width), 0),
                            24));
                            
                        addConditionPresenter.Arrange(new Rect(25,
                            y + eventsConditionsHeight - 26,
                            ConditionWidth(finalSize.Width),
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
                if (draggingEvents && GetSelected(eventPresenter.Child))
                {
                    float conditionsHeight = ArrangeConditions(eventIndex, 20, finalSize, eventPresenter, start, height);
                    height = Math.Max(height + conditionsHeight, ArrangeActions(eventIndex, 20, finalSize, eventPresenter, start, height));
                    eventPresenter.Arrange(new Rect(20, start, EventWidth(finalSize.Width), height));
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

        public int OverIndexEvent { get; set; }

        public SmartScript Script
        {
            get => (SmartScript) GetValue(ScriptProperty);
            set => SetValue(ScriptProperty, value);
        }

        public static readonly AvaloniaProperty ScriptProperty =
            AvaloniaProperty.Register<SmartScriptPanelLayout, SmartScript>(nameof(Script),
                null);
        
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

        public static readonly AvaloniaProperty DropActionsProperty = AvaloniaProperty.Register<SmartScriptPanelLayout, ICommand>(nameof(DropActions)
            );

        public ICommand DropActions
        {
            get => (ICommand) GetValue(DropActionsProperty);
            set => SetValue(DropActionsProperty, value);
        }

        public static readonly AvaloniaProperty DropConditionsProperty = AvaloniaProperty.Register<SmartScriptPanelLayout, ICommand>(nameof(DropConditions)
            );

        public ICommand DropConditions
        {
            get => (ICommand) GetValue(DropConditionsProperty);
            set => SetValue(DropConditionsProperty, value);
        }

    }
}