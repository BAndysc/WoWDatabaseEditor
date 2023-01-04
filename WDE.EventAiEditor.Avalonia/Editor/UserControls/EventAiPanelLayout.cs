using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.EventAiEditor.Editor.ViewModels;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    internal class EventAiPanelLayout : RenderedPanel
    {
        private readonly List<(float y, float height, int actionIndex, int eventIndex)> actionHeights = new();
        private readonly List<(float y, float height, int conditionIndex, int eventIndex)> conditionHeights = new();
        private readonly List<(float y, float height, int eventIndex)> eventHeights = new();
        
        private readonly Dictionary<EventAiAction, ContentPresenter> actionToPresenter = new();
        private readonly Dictionary<EventAiEvent, ContentPresenter> eventToPresenter = new();
        private readonly Dictionary<ContentPresenter, EventAiAction> presenterToAction = new();
        private readonly Dictionary<ContentPresenter, EventAiEvent> presenterToEvent = new();

        private ContentPresenter? addActionPresenter;
        private NewActionViewModel? addActionViewModel;
        
        private bool draggingActions;
        private bool draggingEvents;
        private bool isCopying;

        private Point mouseStartPosition;
        private float mouseY;

        public (float y, float height, int eventIndex) OverIndexEvent { get; set; }
        private (float y, float height, int actionIndex, int eventIndex) overIndexAction;

        private static double PaddingLeft = 20;
        
        private double EventWidth(double totalWidth) => Math.Min(Math.Max(totalWidth - 50, PaddingLeft + 10), 250);

        static EventAiPanelLayout()
        {
            AffectsRender<EventAiPanelLayout>(ProblemsProperty);
            PointerPressedEvent.AddClassHandler<EventAiPanelLayout>(PointerPressedHandled, RoutingStrategies.Tunnel, true);
        }

        private static void PointerPressedHandled(EventAiPanelLayout panel, PointerPressedEventArgs e)
        {
            panel.mouseStartPosition = e.GetPosition(panel);
        }

        public EventAiPanelLayout()
        {
            Children.CollectionChanged += ChildrenCollectionChanged;
        }

        protected void ChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
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
                if (visualRemovedPresenter.Content is EventAiEvent @event)
                {
                    presenterToEvent.Remove(visualRemovedPresenter);
                    eventToPresenter.Remove(@event);
                }
                else if (visualRemovedPresenter.Content is EventAiAction action)
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

        private void OnLoadVisualChild(object? sender, EventArgs e)
        {
            if (sender is not ContentPresenter visualAddedPresenter)
                return;
            
            if (visualAddedPresenter.Content is EventAiEvent @event)
            {
                presenterToEvent[visualAddedPresenter] = @event;
                eventToPresenter[@event] = visualAddedPresenter;
            }
            else if (visualAddedPresenter.Content is EventAiAction action)
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
            foreach (var child in Children)
            {
                if (child is ContentPresenter { Content: EventAiEvent } cp)
                    yield return cp;
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            float totalDesiredHeight = 0;
            double eventWidth = EventWidth((float) availableSize.Width);
            double actionWidth = availableSize.Width - eventWidth;
            
            foreach (ContentPresenter eventPresenter in Events())
            {
                eventPresenter.Measure(new Size(eventWidth - PaddingLeft, availableSize.Height));

                float actionsHeight = 26;

                if (!presenterToEvent.ContainsKey(eventPresenter))
                    continue;

                EventAiEvent eventAiEvent = presenterToEvent[eventPresenter];
                foreach (EventAiAction action in eventAiEvent.Actions)
                {
                    if (!actionToPresenter.TryGetValue(action, out var actionPresenter))
                        continue;

                    actionPresenter.Measure(new Size(actionWidth, availableSize.Height));

                    actionsHeight += (float) actionPresenter.DesiredSize.Height + ActionSpacing;
                }
                
                totalDesiredHeight += Math.Max(actionsHeight, (float) eventPresenter.DesiredSize.Height) + EventSpacing;
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
                foreach (var @event in Children)
                {
                    if (@event is not ContentPresenter cp)
                        continue;
                    if (cp.Child != null)
                        SetSelected(cp.Child, false);
                }
            }            
        }

        private void UpdateIsCopying(KeyModifiers key)
        {
            var systemWideControlModifier = KeyGestures.CommandModifier;
            isCopying = key.HasFlagFast(systemWideControlModifier);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
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
            }

            draggingEvents = false;
            draggingActions = false;
            InvalidateArrange();
            InvalidateVisual();
        }

        private static FormattedText? vvvvText;
        private static FormattedTextNumberCache NumberCache = new();

        public override void Render(DrawingContext dc)
        {
            if (AnythingSelected())
            {
                if (draggingActions)
                {
                    double x = EventWidth(Bounds.Width);
                    float y = overIndexAction.y - overIndexAction.height / 2 - 1;
                    dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(x, y), new Point(x + 200, y));
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
                vvvvText = new FormattedText("vvv", CultureInfo.CurrentCulture,FlowDirection.LeftToRight,Typeface.Default, 7, null);
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
                    dc.DrawText(ft, new Point(0, yPos + 5)); // Brushes.DarkGray;
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
                        dc.DrawText(ft, new Point(0, yPos + 5)); // Brushes.DarkGray;
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
                vvvvText!.SetForegroundBrush(severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical ? Brushes.Red : Brushes.Orange);
                dc.DrawText(vvvvText, new Point(0, yPos + 5 + 10));   
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
                !draggingActions && !draggingEvents)
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
                    }
                    else
                        draggingActions = AnyActionSelected();
            
                    if (draggingEvents || draggingActions)
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

            InvalidateArrange();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            mouseStartPositionValid = false;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            eventHeights.Clear();
            actionHeights.Clear();
            conditionHeights.Clear();
            float y = EventSpacing;

            if (draggingActions || draggingEvents)
            {
                addActionPresenter?.Arrange(new Rect(-5, -5, 1, 1));
            }
            
            float lastHeight = 0;
            float selectedHeight = 0;
            int eventIndex = 0;
            foreach (EventAiEvent ev in Script.Events)
            {
                eventIndex++;
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                
                lastHeight = Math.Max(MeasureActions(eventPresenter), (float) eventPresenter.DesiredSize.Height);
                eventHeights.Add((y + (lastHeight + EventSpacing) / 2, lastHeight + EventSpacing, eventIndex - 1));
                y += lastHeight + EventSpacing;

                if (!draggingEvents || isCopying || eventPresenter.Child == null || !GetSelected(eventPresenter.Child))
                    continue;
                selectedHeight += lastHeight + EventSpacing;
            }

            eventHeights.Add((y, 0, eventIndex));

            y = EventSpacing;
            float start = 0;
            if (OverIndexEvent.eventIndex == 0)
            {
                start = y;
                y += selectedHeight;
            }

            eventIndex = 0;
            if (addActionViewModel != null)
                addActionViewModel.Event = null;
            
            foreach (EventAiEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;

                var height = (float)eventPresenter.DesiredSize.Height;
                if (!draggingEvents || isCopying || eventPresenter.Child == null || !GetSelected(eventPresenter.Child))
                {
                    float actionHeight = ArrangeActions(eventIndex, 0, finalSize, eventPresenter, y, height);
                    height = Math.Max(height, actionHeight);
                    eventPresenter.Arrange(new Rect(PaddingLeft, y, EventWidth(finalSize.Width) - PaddingLeft, height));

                    if (mouseY > y && mouseY < y + height && !draggingActions && !draggingEvents)
                    {
                        if (presenterToEvent.TryGetValue(eventPresenter, out EventAiEvent? @event) && @event != null)
                        {
                            if (addActionViewModel != null)
                                addActionViewModel.Event = @event;
                        }
                            
                        addActionPresenter?.Arrange(new Rect(EventWidth(finalSize.Width),
                            y + actionHeight - 26,
                            Math.Max(finalSize.Width - EventWidth(finalSize.Width), 0),
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
            foreach (EventAiEvent ev in Script.Events)
            {
                if (!eventToPresenter.TryGetValue(ev, out var eventPresenter))
                    continue;
                var height = (float) eventPresenter.DesiredSize.Height;
                if (draggingEvents && !isCopying && eventPresenter.Child != null && GetSelected(eventPresenter.Child))
                {
                    height = Math.Max(height, ArrangeActions(eventIndex, 20, finalSize, eventPresenter, start, height));
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
            if (!presenterToEvent.TryGetValue(eveentPresenter, out EventAiEvent? @event))
                return totalHeight;

            var actionIndex = 0;
            foreach (EventAiAction action in @event.Actions)
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
            if (!presenterToEvent.TryGetValue(eventPresenter, out EventAiEvent? @event))
                return totalHeight;

            foreach (EventAiAction action in @event.Actions)
            {
                if (!actionToPresenter.TryGetValue(action, out ContentPresenter? actionPresenter))
                    continue;

                var height = (float) actionPresenter.DesiredSize.Height;
                totalHeight += height + ActionSpacing;
            }

            return totalHeight;
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            InvalidateMeasure();
            InvalidateArrange();
        }

        public EventAiScript Script
        {
            get => (EventAiScript?) GetValue(ScriptProperty) ?? throw new NullReferenceException();
            set => SetValue(ScriptProperty, value);
        }

        public static readonly AvaloniaProperty ScriptProperty =
            AvaloniaProperty.Register<EventAiPanelLayout, EventAiScript?>(nameof(Script), null);
        
        public Dictionary<int, DiagnosticSeverity>? Problems
        {
            get => (Dictionary<int, DiagnosticSeverity>?) GetValue(ProblemsProperty);
            set => SetValue(ProblemsProperty, value);
        }
        public static readonly AvaloniaProperty ProblemsProperty =
            AvaloniaProperty.Register<EventAiPanelLayout, Dictionary<int, DiagnosticSeverity>?>(nameof(Problems));

        public float EventSpacing => 10;

        public float ActionSpacing => 2;

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.RegisterAttached<EventAiPanelLayout, Control, bool>("Selected");
        public static bool GetSelected(Control control) => (bool?)control.GetValue(SelectedProperty) ?? false;
        public static void SetSelected(Control control, bool value) => control.SetValue(SelectedProperty, value);

        public static readonly AvaloniaProperty DropItemsProperty = AvaloniaProperty.Register<EventAiPanelLayout, ICommand>(nameof(DropItems));

        public ICommand DropItems
        {
            get => (ICommand?) GetValue(DropItemsProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DropItemsProperty, value);
        }

        public static readonly AvaloniaProperty DropActionsProperty = AvaloniaProperty.Register<EventAiPanelLayout, ICommand>(nameof(DropActions));

        public ICommand DropActions
        {
            get => (ICommand?) GetValue(DropActionsProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DropActionsProperty, value);
        }
    }
}
