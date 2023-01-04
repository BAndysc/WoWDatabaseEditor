using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using WDE.Common.Types;

namespace WDE.Common.Avalonia.Components
{
    public class ButtonImage : Button
    {
        private object? image;
        public static readonly DirectProperty<ButtonImage, object?> ImageProperty = AvaloniaProperty.RegisterDirect<ButtonImage, object?>("Image", o => o.Image, (o, v) => o.Image = v);
        
        private string? text;
        public static readonly DirectProperty<ButtonImage, string?> TextProperty = AvaloniaProperty.RegisterDirect<ButtonImage, string?>("Text", o => o.Text, (o, v) => o.Text = v);
        protected override Type StyleKeyOverride => typeof(Button);

        public object? Image
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
            TextProperty.Changed.AddClassHandler<ButtonImage>(UpdateContentTemplate);
            ImageProperty.Changed.AddClassHandler<ButtonImage>(UpdateContentTemplate);
        }

        // this doesn't make full sense, but it works
        // this doesn't make sense, because I am setting ContentTemplate with hardcoded text and image
        // for some reason when I tried to set Content directly, then overflowing in ToolbarPanel didn't fully work
        // (icons were disappearing randomly, especially when toggling between Icon/Text view)
        // this workaround seems to be working
        private static void UpdateContentTemplate(ButtonImage btn, AvaloniaPropertyChangedEventArgs arg2)
        {
            var image = btn.Image;
            var imageUri = image as ImageUri?;
            var imageString = image as string;
            var hasImage = imageUri != null || imageString != null;
            var text = btn.Text;
            
            if (hasImage && text != null)
            {
                btn.ContentTemplate = new FuncDataTemplate(_ => true, (_, _) =>
                {
                    var sp = new StackPanel(){ Orientation = Orientation.Horizontal };
                    var imageControl = new WdeImage();
                    imageControl[!IsVisibleProperty] = new DynamicResourceExtension("DisplayButtonImageIcon");
                    if (imageUri.HasValue)
                        imageControl.Image = imageUri.Value;
                    else
                        imageControl.ImageUri = imageString;
                    var textBlock = new TextBlock(){Text = text, VerticalAlignment = VerticalAlignment.Center};
                    textBlock[!IsVisibleProperty] = new DynamicResourceExtension("DisplayButtonImageText");
                    sp.Children.Add(imageControl);
                    sp.Children.Add(textBlock);
                    return sp;
                });
            }
            else if (hasImage)
            {
                btn.ContentTemplate = new FuncDataTemplate(_ => true, (_, _) =>
                {
                    var imageControl = new WdeImage();
                    if (imageUri.HasValue)
                        imageControl.Image = imageUri.Value;
                    else
                        imageControl.ImageUri = imageString;
                    return imageControl;
                });
            }
            else if (text != null)
            {
                btn.ContentTemplate = new FuncDataTemplate(_ => true, (_, _) => new TextBlock() { Text = text, VerticalAlignment = VerticalAlignment.Center});
            }
        }
    }
}