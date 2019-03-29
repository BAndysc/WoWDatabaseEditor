using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;

using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.Helpers
{
    class SmartScriptButton
    {
        public Rectangle rectangle { get; private set; }
        public TextBlock buttonText { get; private set; }

        private INotifiableSmartElement owner;

        public SmartScriptButton(int x, int y, string text, INotifiableSmartElement o, bool useEventWidth = false)
        {
            owner = o;
            InitRectangle(x, y, useEventWidth);
            InitText(x, y, text);
        }

        private void InitRectangle(int x, int y, bool useEventWidth)
        {
            rectangle = new Rectangle();
            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, y);

            rectangle.Fill = Brushes.Transparent;
            rectangle.Stroke = Brushes.Black;
            rectangle.StrokeThickness = 1;

            rectangle.Width = useEventWidth ? SmartEventVisual.eventWidth : SmartActionVisual.actionWidth;
            rectangle.Height = SmartEventVisual.actionHeight;

            rectangle.MouseLeftButtonDown += OnMouseLeftClick;
            rectangle.IsMouseDirectlyOverChanged += OnMouseHover;
        }

        private void InitText(int x, int y, string text)
        {
            buttonText = new TextBlock();
            buttonText.Text = text;
            buttonText.Foreground = Brushes.Black;
            buttonText.Width = buttonText.MaxWidth = 400;
            Canvas.SetLeft(buttonText, x + 4);
            Canvas.SetTop(buttonText, y + 3);
            buttonText.MouseLeftButtonDown += OnMouseLeftClick;
            buttonText.IsMouseDirectlyOverChanged += OnMouseHover;
        }

        private void OnMouseHover(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
                rectangle.Fill = Brushes.DimGray;
            else
                rectangle.Fill = Brushes.Transparent;
        }

        private void OnMouseLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                owner.NotifyElementAboutEvent();
        }
    }
}
