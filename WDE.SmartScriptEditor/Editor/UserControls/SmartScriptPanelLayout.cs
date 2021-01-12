using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    internal class SmartScriptPanelLayout : Panel
    {
        private readonly List<(float y, float height, int actionIndex, int eventIndex)> actionHeights = new();

        private readonly Dictionary<SmartAction, ContentPresenter> actionToPresenter = new();

        private readonly List<float> eventHeights = new();
        private readonly Dictionary<SmartEvent, ContentPresenter> eventToPresenter = new();

        private readonly Dictionary<ContentPresenter, SmartAction> presenterToAction = new();

        private readonly Dictionary<ContentPresenter, SmartEvent> presenterToEvent = new();

        private ContentPresenter addActionPresenter;
        private NewActionViewModel addActionViewModel;
        private bool draggingActions;

        private bool draggingEvents;

        private Point mouseStartPosition;
        private float mouseY;

        private (float y, float height, int actionIndex, int eventIndex) overIndexAction;

        private float EventWidth => Math.Min(Math.Max((float) ActualWidth - 50, 0), 250);

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
                else if (visualRemovedPresenter.Content is NewActionViewModel)
                {
                    addActionPresenter = null;
                    addActionViewModel = null;
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
            else if (visualAddedPresenter.Content is NewActionViewModel vm)
            {
                addActionPresenter = visualAddedPresenter;
                addActionViewModel = vm;
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

        protected override Size MeasureOverride(Size availableSize)
        {
            Size s = base.MeasureOverride(availableSize);
            float totalDesiredHeight = EventSpacing;
            foreach (ContentPresenter eventPresenter in Events())
            {
                eventPresenter.Measure(new Size(EventWidth, availableSize.Height));

                float actionsHeight = 26;

                if (!presenterToEvent.ContainsKey(eventPresenter))
                    continue;

                SmartEvent @event = presenterToEvent[eventPresenter];
                foreach (SmartAction action in @event.Actions)
                {
                    if (!actionToPresenter.ContainsKey(action))
                        continue;

                    ContentPresenter actionPresenter = actionToPresenter[action];
                    actionPresenter.Measure(new Size(ActualWidth - EventWidth, availableSize.Height));

                    actionsHeight += (float) actionPresenter.DesiredSize.Height + ActionSpacing;
                }

                totalDesiredHeight += Math.Max(actionsHeight, (float) eventPresenter.DesiredSize.Height) + EventSpacing;
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
                DropActions?.Execute(new DropActionsArgs
                    {EventIndex = overIndexAction.eventIndex, ActionIndex = overIndexAction.actionIndex});
            }

            draggingEvents = false;
            draggingActions = false;
            InvalidateArrange();
            InvalidateVisual();
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
        }

        private bool AnyActionSelected()
        {
            return actionToPresenter.Keys.Any(a => a.IsSelected);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseY = (float) e.GetPosition(this).Y;
            if (e.LeftButton == MouseButtonState.Pressed && !draggingActions && !draggingEvents)
            {
                var dist = (float) Point.Subtract(mouseStartPosition, e.GetPosition(this)).Length;
                if (dist > 10)
                {
                    if (e.GetPosition(this).X < EventWidth)
                        draggingEvents = true;
                    else
                        draggingActions = AnyActionSelected();
                    if (draggingEvents || draggingActions)
                        CaptureMouse();
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
                    OverIndexEvent = eventHeights.Count > 0 && mouseY > eventHeights[eventHeights.Count - 1]
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

            InvalidateArrange();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            eventHeights.Clear();
            actionHeights.Clear();
            float y = EventSpacing;

            float selectedHeight = 0;
            foreach (ContentPresenter child in Events())
            {
                float height = Math.Max(MeasureActions(child), (float) child.DesiredSize.Height);
                eventHeights.Add(y + (height + EventSpacing) / 2);
                y += height + EventSpacing;

                if (!draggingEvents || !GetSelected(child))
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
            foreach (ContentPresenter child in Events())
            {
                var height = (float) child.DesiredSize.Height;
                if (!draggingEvents || !GetSelected(child))
                {
                    float actionHeight = ArrangeActions(eventIndex, 0, finalSize, child, y, height);
                    height = Math.Max(height, actionHeight);
                    child.Arrange(new Rect(0, y, EventWidth, height));

                    if (mouseY > y && mouseY < y + height && !draggingActions && !draggingEvents)
                    {
                        if (addActionViewModel != null && presenterToEvent.TryGetValue(child, out SmartEvent smartEvent))
                            addActionViewModel.Event = smartEvent;
                        addActionPresenter.Arrange(new Rect(EventWidth,
                            y + actionHeight - 26,
                            Math.Max(finalSize.Width - EventWidth, 0),
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
            foreach (ContentPresenter eventPresenter in Events())
            {
                var height = (float) eventPresenter.DesiredSize.Height;
                if (draggingEvents && GetSelected(eventPresenter))
                {
                    height = Math.Max(height, ArrangeActions(eventIndex, 20, finalSize, eventPresenter, start, height));
                    eventPresenter.Arrange(new Rect(20, start, EventWidth, height));
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

        public float EventSpacing
        {
            get => (float) GetValue(EventSpacingProperty);
            set => SetValue(EventSpacingProperty, value);
        }

        public static readonly DependencyProperty EventSpacingProperty = DependencyProperty.Register(nameof(EventSpacing),
            typeof(float),
            typeof(SmartScriptPanelLayout),
            new PropertyMetadata(10f));

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

        #endregion
    }
}