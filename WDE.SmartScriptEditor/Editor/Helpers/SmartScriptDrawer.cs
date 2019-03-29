using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;

using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.Editor.Helpers
{
    public class SmartScriptDrawer : INotifiableSmartElement
    {
        private Canvas canvas;
        public SmartScriptEditorViewModel dataContext { get; private set; }

        private static readonly int eventPadding = 10;

        public SmartScriptDrawer(Canvas c, SmartScriptEditorViewModel model)
        {
            canvas = c;
            dataContext = model;
            canvas.MouseLeftButtonDown += OnMouseLeftClick;
            RegisterCollectionChangeHandler();
        }

        public void Draw()
        {
            // clear all previous children in order to clean canvas area
            canvas.Children.Clear();

            int eventPosX = 10;
            int eventPosY = 10;

            foreach (var e in dataContext.Events)
            {
                int eventHeight = DrawEvent(e, eventPosX, eventPosY);
                eventPosY += eventHeight + eventPadding;
            }

            SmartScriptButton scriptButton = new SmartScriptButton(eventPosX, eventPosY, "Add Event", this, true);
            canvas.Children.Add(scriptButton.rectangle);
            canvas.Children.Add(scriptButton.buttonText);
        }

        private int DrawEvent(SmartEvent e, int x, int y)
        {
            SmartEventVisual eventVisual = new SmartEventVisual(this, e);
            if (dataContext.SelectedItem == e)
                eventVisual.isSelected = true;

            eventVisual.SetRectangle(x, y);
            eventVisual.DrawActions(canvas);
            eventVisual.InitConditions();

            canvas.Children.Add(eventVisual.rectangle);
            canvas.Children.Add(eventVisual.eventDesc);
            canvas.Children.Add(eventVisual.condRec);

            return eventVisual.GetOverallHeigh();
        }

        private void RegisterCollectionChangeHandler()
        {
            foreach (var e in dataContext.Events)
                e.Actions.CollectionChanged += OnEventsCollectionChange;

            dataContext.Events.CollectionChanged += OnEventsCollectionChange;
        }

        public void NotifyElementAboutEvent()
        {
            dataContext.AddEvent.Execute();
        }

        private void OnMouseLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is Canvas))
                return;

            if (dataContext.SelectedItem != null || dataContext.SelectedAction != null)
            {
                dataContext.SelectedItem = null;
                dataContext.SelectedAction = null;
                Draw();
            }
        }

        private void OnEventsCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var elem in e.NewItems)
                {
                    if (elem is SmartEvent)
                        ((SmartEvent)elem).Actions.CollectionChanged += OnEventsCollectionChange;
                }
            }

            Draw();
        }
    }
}
