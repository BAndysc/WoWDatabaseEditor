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
    class SmartScriptPanelLayout : Panel
    {
        #region Properties

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.RegisterAttached("Selected", typeof(bool),
                typeof(SmartScriptPanelLayout), new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsParentArrange));

        [AttachedPropertyBrowsableForChildren]
        public static bool GetSelected(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool) element.GetValue(SelectedProperty);
        }

        [AttachedPropertyBrowsableForChildren]
        public static void SetSelected(UIElement element, bool length)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(SelectedProperty, length);
        }

        public int OverIndexEvent
        {
            get { return (int) GetValue(OverIndexEventProperty); }
            set { SetValue(OverIndexEventProperty, value); }
        }

        public static readonly DependencyProperty OverIndexEventProperty =
            DependencyProperty.Register(nameof(OverIndexEvent), typeof(int),
                typeof(SmartScriptPanelLayout), new PropertyMetadata(0));

        public float EventSpacing
        {
            get => (float) GetValue(EventSpacingProperty);
            set => SetValue(EventSpacingProperty, value);
        }

        public static readonly DependencyProperty EventSpacingProperty =
            DependencyProperty.Register(nameof(EventSpacing), typeof(float),
                typeof(SmartScriptPanelLayout), new PropertyMetadata(10f));

        public float ActionSpacing
        {
            get => (float) GetValue(ActionSpacingProperty);
            set => SetValue(ActionSpacingProperty, value);
        }

        public static readonly DependencyProperty ActionSpacingProperty =
            DependencyProperty.Register(nameof(ActionSpacing), typeof(float),
                typeof(SmartScriptPanelLayout), new PropertyMetadata(2f));

        public static readonly DependencyProperty DropItemsProperty =
            DependencyProperty.Register(
                nameof(DropItems),
                typeof(ICommand),
                typeof(SmartScriptPanelLayout),
                new UIPropertyMetadata(null));

        public ICommand DropItems
        {
            get => (ICommand) GetValue(DropItemsProperty);
            set => SetValue(DropItemsProperty, value);
        }

        public static readonly DependencyProperty DropActionsProperty =
            DependencyProperty.Register(
                nameof(DropActions),
                typeof(ICommand),
                typeof(SmartScriptPanelLayout),
                new UIPropertyMetadata(null));

        public ICommand DropActions
        {
            get => (ICommand) GetValue(DropActionsProperty);
            set => SetValue(DropActionsProperty, value);
        }

        #endregion

        private (float y, float height, int actionIndex, int eventIndex) overIndexAction;

        private bool draggingEvents = false;
        private bool draggingActions = false;

        private Point mouseStartPosition;

        Dictionary<ContentPresenter, SmartEvent> presenterToEvent = new Dictionary<ContentPresenter, SmartEvent>();
        Dictionary<SmartEvent, ContentPresenter> eventToPresenter = new Dictionary<SmartEvent, ContentPresenter>();

        Dictionary<ContentPresenter, SmartAction> presenterToAction = new Dictionary<ContentPresenter, SmartAction>();
        Dictionary<SmartAction, ContentPresenter> actionToPresenter = new Dictionary<SmartAction, ContentPresenter>();

        private ContentPresenter addActionPresenter;
        private NewActionViewModel addActionViewModel;
        
        List<float> eventHeights = new List<float>();

        private List<(float y, float height, int actionIndex, int eventIndex)> actionHeights =
            new List<(float, float, int, int)>();

        float eventWidth => Math.Min(Math.Max((float) ActualWidth - 50, 0), 250);
        private float mouseY;

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
            var visualAddedPresenter = sender as ContentPresenter;
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
            var s = base.MeasureOverride(availableSize);
            float totalDesiredHeight = EventSpacing;
            foreach (ContentPresenter eventPresenter in Events())
            {
                eventPresenter.Measure(new Size(eventWidth, availableSize.Height));

                float actionsHeight = 26;

                if (!presenterToEvent.ContainsKey(eventPresenter))
                    continue;

                var @event = presenterToEvent[eventPresenter];
                foreach (var action in @event.Actions)
                {
                    if (!actionToPresenter.ContainsKey(action))
                        continue;

                    var actionPresenter = actionToPresenter[action];
                    actionPresenter.Measure(new Size(ActualWidth - eventWidth, availableSize.Height));

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
                    foreach (var @event in Events())
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
            this.ReleaseMouseCapture();
            if (draggingEvents)
                DropItems?.Execute(OverIndexEvent);
            else if (draggingActions)
                DropActions?.Execute(new DropActionsArgs()
                    {EventIndex = overIndexAction.eventIndex, ActionIndex = overIndexAction.actionIndex});
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
                float x = eventWidth;
                float y = overIndexAction.y - overIndexAction.height / 2 - 1;
                dc.DrawLine(new Pen(Brushes.Gray, 1),
                    new Point(x, y), new Point(x + 200, y));
            }
        }

        private bool AnyActionSelected() => actionToPresenter.Keys.Any(a => a.IsSelected);
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseY = (float) e.GetPosition(this).Y;
            if (e.LeftButton == MouseButtonState.Pressed && !draggingActions && !draggingEvents)
            {
                float dist = (float) Point.Subtract(mouseStartPosition, e.GetPosition(this)).Length;
                if (dist > 10)
                {
                    if (e.GetPosition(this).X < eventWidth)
                        draggingEvents = true;
                    else
                        draggingActions = AnyActionSelected();
                    if (draggingEvents || draggingActions)
                        this.CaptureMouse();
                }
            }

            if (draggingEvents)
            {
                int eventIndex = 0;
                bool found = false;
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
                    OverIndexEvent = (eventHeights.Count > 0 && mouseY > eventHeights[eventHeights.Count - 1])
                        ? eventHeights.Count - 1
                        : 0;
            }
            else if (draggingActions)
            {
                foreach (var tuple in actionHeights)
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

            int eventIndex = 0;
            if (addActionViewModel != null)
                addActionViewModel.Event = null;
            foreach (ContentPresenter child in Events())
            {
                float height = (float) child.DesiredSize.Height;
                if (!draggingEvents || !GetSelected(child))
                {
                    var actionHeight = ArrangeActions(eventIndex, 0, finalSize, child, y, height);
                    height = Math.Max(height, actionHeight);
                    child.Arrange(new Rect(0, y, eventWidth, height));

                    if (mouseY > y && mouseY < y + height && !draggingActions && !draggingEvents)
                    {
                        if (addActionViewModel != null && presenterToEvent.TryGetValue(child, out var smartEvent))
                            addActionViewModel.Event = smartEvent;
                        addActionPresenter.Arrange(new Rect(eventWidth, y + actionHeight - 26, Math.Max(finalSize.Width - eventWidth, 0), 24));                        
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
                float height = (float) eventPresenter.DesiredSize.Height;
                if (draggingEvents && GetSelected(eventPresenter))
                {
                    height = Math.Max(height, ArrangeActions(eventIndex, 20, finalSize, eventPresenter, start, height));
                    eventPresenter.Arrange(new Rect(20, start, eventWidth, height));
                    start += height + EventSpacing;
                }

                eventIndex++;
            }

            return finalSize;
        }

        private float ArrangeActions(int eventIndex, float x, Size totalSize, ContentPresenter eveentPresenter, float y, float eventHeight)
        {
            float totalHeight = 26;
            if (!presenterToEvent.TryGetValue(eveentPresenter, out var @event))
                return totalHeight;

            int actionIndex = 0;
            foreach (var action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out var actionPresenter))
                    continue;

                float height = (float) actionPresenter.DesiredSize.Height;
                actionPresenter.Arrange(new Rect(eventWidth + x, y, Math.Max(totalSize.Width - eventWidth, 0), height));
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
            if (!presenterToEvent.TryGetValue(eventPresenter, out var @event))
                return totalHeight;

            foreach (var action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out var actionPresenter))
                    continue;

                float height = (float) actionPresenter.DesiredSize.Height;
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
    }
}