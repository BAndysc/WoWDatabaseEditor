using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using WDE.Common.Types;

namespace AvaloniaStyles.Demo;

public class ButtonImageDemo : Button
{
    private object? image;
    public static readonly DirectProperty<ButtonImageDemo, object?> ImageProperty = AvaloniaProperty.RegisterDirect<ButtonImageDemo, object?>("Image", o => o.Image, (o, v) => o.Image = v);
    
    private string? text;
    public static readonly DirectProperty<ButtonImageDemo, string?> TextProperty = AvaloniaProperty.RegisterDirect<ButtonImageDemo, string?>("Text", o => o.Text, (o, v) => o.Text = v);
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

    static ButtonImageDemo()
    {
        TextProperty.Changed.AddClassHandler<ButtonImageDemo>(UpdateContent);
        ImageProperty.Changed.AddClassHandler<ButtonImageDemo>(UpdateContent);
    }

    public ButtonImageDemo()
    {
    }

    private static void UpdateContent(ButtonImageDemo btn, AvaloniaPropertyChangedEventArgs args)
    {
        var image = btn.Image;
        var imageUri = image as ImageUri?;
        var imageString = image as string;
        var hasImage = imageUri != null || imageString != null;
        var text = btn.Text;

        if (hasImage && text != null)
        {
            var sp = new StackPanel() { Orientation = Orientation.Horizontal };
            var tb = new TextBlock() { Text = text, VerticalAlignment = VerticalAlignment.Center };
            tb.Classes.Add("ButtonText");
            sp.Children.Add(tb);
            btn.Content = sp;
        }
        else if (hasImage)
        {
            var imageControl = new Image();
            btn.Content = imageControl;
        }
        else if (text != null)
        {
            btn.Content = new TextBlock() { Text = text, VerticalAlignment = VerticalAlignment.Center};
        }
    }
}