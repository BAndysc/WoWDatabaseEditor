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
    //     Object used to visualize SmartAction object at smart script canvas
    class SmartActionVisual
    {
        public Rectangle rectangle { get; private set; }
        public TextBlock actionDesc { get; private set; }
        public bool isSelected { get; set; }

        private SmartAction smartAction;
        private SmartEventVisual parent;

        public static readonly int actionWidth = 500;

        public SmartActionVisual(SmartEventVisual p, SmartAction sa)
        {
            parent = p;
            smartAction = sa;
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
                rectangle.Fill = Brushes.LightGoldenrodYellow;
                rectangle.Stroke = Brushes.Orange;
                rectangle.StrokeThickness = 3;
            }
            else
            {
                rectangle.Fill = Brushes.LightSkyBlue;
                rectangle.Stroke = Brushes.White;
                rectangle.StrokeThickness = 1;
            }

            InitActionDescription(x, y);

            rectangle.Width = actionWidth;
            rectangle.Height = SmartEventVisual.actionHeight;
            
            rectangle.MouseLeftButtonDown += OnMouseLeftClick;
        }

        public void OnMouseLeftClick(object sender, MouseButtonEventArgs e)
        {
            parent.InvokeActionSelection(smartAction);

            if (e.ClickCount == 2)
                parent.parent.dataContext.EditAction.Execute(smartAction);
        }

        private void InitActionDescription(int x, int y)
        {
            actionDesc = new TextBlock();
            actionDesc.Text = smartAction.Readable;
            actionDesc.Foreground = Brushes.Black;
            actionDesc.Width = actionDesc.MaxWidth = 400;
            Canvas.SetLeft(actionDesc, x + 4);
            Canvas.SetTop(actionDesc, y + 3);
            actionDesc.MouseLeftButtonDown += OnMouseLeftClick;
        }
    }
}
