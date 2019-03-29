using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;

using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.Helpers
{
    //
    // Summary:
    //     Object used to visualize SmartEvent object at smart script canvas
    class SmartEventVisual : INotifiableSmartElement
    {
        public SmartScriptDrawer parent { get; private set; }
        public Rectangle rectangle { get; private set; }
        public Rectangle condRec { get; private set; }
        public TextBlock eventDesc { get; private set; }
        //public TextBlock condDesc { get; private set; }
        public bool isSelected { get; set; }
        
        private SmartEvent smartEvent;
        private Point position;
        private int condRecHeight;

        public static readonly int eventWidth = 200;
        public static readonly int actionHeight = 25;
        public static readonly int actionPadding = 2;

        public SmartEventVisual(SmartScriptDrawer p, SmartEvent e)
        {
            parent = p;
            smartEvent = e;
            isSelected = false;
        }

        public void SetRectangle(int x, int y)
        {
            rectangle = new Rectangle();
            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, y);

            // @TODO: Select proper colors
            if (isSelected)
            {
                rectangle.Fill = Brushes.LightGray;
                rectangle.Stroke = Brushes.Orange;
                rectangle.StrokeThickness = 3;
            }
            else
            {
                rectangle.Fill = Brushes.LightGray;
                rectangle.Stroke = Brushes.White;
                rectangle.StrokeThickness = 2;
            }

            InitEventDescription(x, y);

            rectangle.Width = eventWidth;
            rectangle.Height = eventDesc.DesiredSize.Height + 10;
            condRecHeight = 10;

            rectangle.MouseLeftButtonDown += OnMouseLeftClick;
            position = new Point(x, y);
        }

        private void InitEventDescription(int x, int y)
        {
            eventDesc = new TextBlock();
            eventDesc.TextWrapping = TextWrapping.Wrap;
            eventDesc.Text = smartEvent.Readable;
            eventDesc.Foreground = Brushes.Black;
            eventDesc.Width = eventDesc.MaxWidth = eventWidth - 11;
            Canvas.SetLeft(eventDesc, x + 6);
            Canvas.SetTop(eventDesc, y + 3);
            eventDesc.Measure(new Size(eventWidth, eventWidth));
            eventDesc.MouseLeftButtonDown += OnMouseLeftClick;
        }

        public void InitConditions()
        {
            // Create block for condition
            condRec = new Rectangle();
            Canvas.SetLeft(condRec, position.X + 20);
            Canvas.SetTop(condRec, position.Y + rectangle.Height);

            condRec.Fill = isSelected ? Brushes.LightGoldenrodYellow : Brushes.GhostWhite;
            condRec.Stroke = Brushes.Black;
            condRec.StrokeThickness = 0.1;

            condRec.Width = eventWidth - 20;
            condRec.Height = condRecHeight;
        }

        public void DrawActions(Canvas canvas)
        {
            int actionX = (int)position.X + 201;
            int actionY = (int)position.Y;

            foreach (var action in smartEvent.Actions)
            {
                SmartActionVisual actionVisual = new SmartActionVisual(this, action);

                if (isSelected || action == parent.dataContext.SelectedAction)
                    actionVisual.isSelected = true;

                actionVisual.SetRectangle(actionX, actionY);

                actionY += actionHeight + actionPadding;
                canvas.Children.Add(actionVisual.rectangle);
                canvas.Children.Add(actionVisual.actionDesc);
            }

            SmartScriptButton button = new SmartScriptButton(actionX, actionY, "Add action", this);
            canvas.Children.Add(button.rectangle);
            canvas.Children.Add(button.buttonText);
            actionY += actionHeight;

            if ((actionY - actionPadding) > (rectangle.Height + position.Y))
                condRecHeight = actionY - (int)(rectangle.Height + position.Y);
        }

        private void OnMouseLeftClick(object sender, MouseButtonEventArgs e)
        {
            InvokeEventSelection();

            if (e.ClickCount == 2)
                parent.dataContext.EditEvent.Execute();
        }

        private void InvokeEventSelection()
        {
            parent.dataContext.SelectedItem = smartEvent;
            parent.dataContext.SelectedAction = null;
            parent.Draw();
        }

        public void InvokeActionSelection(SmartAction action)
        {
            parent.dataContext.SelectedItem = null;
            parent.dataContext.SelectedAction = action;
            parent.Draw();
        }

        public void NotifyElementAboutEvent()
        {
            parent.dataContext.AddAction.Execute(smartEvent);
        }

        public int GetOverallHeigh()
        {
            return (int)(rectangle.Height + condRecHeight);
        }
    }
}
