using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Styling;

namespace WDE.Common.Avalonia.Components
{
    public class ButtonImage : Button, IStyleable
    {
        private string? image;
        public static readonly DirectProperty<ButtonImage, string?> ImageProperty = AvaloniaProperty.RegisterDirect<ButtonImage, string?>("Image", o => o.Image, (o, v) => o.Image = v);
        
        private string? text;
        public static readonly DirectProperty<ButtonImage, string?> TextProperty = AvaloniaProperty.RegisterDirect<ButtonImage, string?>("Text", o => o.Text, (o, v) => o.Text = v);
        Type IStyleable.StyleKey => typeof(Button);

        public string? Image
        {
            get => image;
            set => SetAndRaise(ImageProperty, ref image, value);
        }

        public string? Text
        {
            get => text;
            set => SetAndRaise(TextProperty, ref text, value);
        }

        static ButtonImage()
        {
            TextProperty.Changed.AddClassHandler<ButtonImage>(UpdateContent);
            ImageProperty.Changed.AddClassHandler<ButtonImage>(UpdateContent);
        }

        private static void UpdateContent(ButtonImage btn, AvaloniaPropertyChangedEventArgs args)
        {
            var image = btn.Image;
            var text = btn.Text;

            if (image != null && text != null)
            {
                var sp = new StackPanel() { Orientation = Orientation.Horizontal };
                sp.Children.Add(new WdeImage(){ImageUri = image});
                sp.Children.Add(new TextBlock(){Text = text});
                btn.Content = sp;
            }
            else if (image != null)
            {
                btn.Content = new WdeImage(){ImageUri = image};
            }
            else if (text != null)
            {
                btn.Content = new TextBlock() { Text = text };
            }
        }
    }
}