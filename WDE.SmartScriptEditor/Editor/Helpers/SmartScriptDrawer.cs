using System;
using System.Collections.ObjectModel;
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
    public class SmartScriptDrawer
    {
        private ObservableCollection<SmartEvent> events;
        private Canvas canvas;
        public SmartScriptEditorViewModel dataContext { get; private set; }

        private static readonly int eventPadding = 10;

        public SmartScriptDrawer(ObservableCollection<SmartEvent> source, Canvas c, SmartScriptEditorViewModel model)
        {
            events = source;
            canvas = c;
            dataContext = model;
            canvas.MouseLeftButtonDown += OnMouseLeftClick;
        }

        public void Draw()
        {
            // clear all previous children in order to clean canvas area
            canvas.Children.Clear();

            int eventPosX = 10;
            int eventPosY = 10;

            foreach (var e in events)
            {
                int eventHeight = DrawEvent(e, eventPosX, eventPosY);
                eventPosY += eventHeight + eventPadding;
            }
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

        public void OnMouseLeftClick(object sender, MouseButtonEventArgs e)
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
    }
}
