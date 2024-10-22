using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Classic.CommonControls;
using WDE.Common.Types;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Components
{
    public class ButtonImage : ToolBarButton
    {
        private object? image;
        public static readonly DirectProperty<ButtonImage, object?> ImageProperty = AvaloniaProperty.RegisterDirect<ButtonImage, object?>("Image", o => o.Image, (o, v) => o.Image = v);

        protected override Type StyleKeyOverride => typeof(ToolBarButton);

        public object? Image
        {
            get => image;
            set => SetAndRaise(ImageProperty, ref image, value);
        }

        static ButtonImage()
        {
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

            if (imageUri != null)
            {
                var img = imageUri.Value;

                async Task Func()
                {
                    var bitmap = await WdeImage.LoadBitmapAsync(img);
                    if (bitmap != null && btn.Image is ImageUri iu && iu == img)
                    {
                        btn.SetCurrentValue(SmallIconProperty, bitmap);
                    }
                    else if (bitmap == null)
                    {
                        btn.ClearValue(SmallIconProperty);
                    }
                }

                Func().ListenErrors();
            }
            else if (imageString != null)
            {
                var img = new ImageUri(imageString);

                async Task Func()
                {
                    var bitmap = await WdeImage.LoadBitmapAsync(img);
                    if (bitmap != null && btn.Image is string iu && iu == img.Uri)
                    {
                        btn.SetCurrentValue(SmallIconProperty, bitmap);
                    }
                    else if (bitmap == null)
                    {
                        btn.ClearValue(SmallIconProperty);
                    }
                }

                Func().ListenErrors();
            }
            else
            {
                btn.ClearValue(SmallIconProperty);
            }

            // var image = btn.Image;
            // var imageUri = image as ImageUri?;
            // var imageString = image as string;
            // var hasImage = imageUri != null || imageString != null;
            // var text = btn.Text;
            //
            // if (hasImage && text != null)
            // {
            //     btn.ContentTemplate = new FuncDataTemplate(_ => true, (_, _) =>
            //     {
            //         var sp = new StackPanel(){ Orientation = Orientation.Horizontal };
            //         var imageControl = new WdeImage();
            //         imageControl[!IsVisibleProperty] = new DynamicResourceExtension("DisplayButtonImageIcon");
            //         if (imageUri.HasValue)
            //             imageControl.Image = imageUri.Value;
            //         else
            //             imageControl.ImageUri = imageString;
            //         var textBlock = new TextBlock(){Text = text, VerticalAlignment = VerticalAlignment.Center, Name = "PART_Text"};
            //         textBlock[!IsVisibleProperty] = new DynamicResourceExtension("DisplayButtonImageText");
            //         sp.Children.Add(imageControl);
            //         sp.Children.Add(textBlock);
            //         return sp;
            //     });
            // }
            // else if (hasImage)
            // {
            //     btn.ContentTemplate = new FuncDataTemplate(_ => true, (_, _) =>
            //     {
            //         var imageControl = new WdeImage();
            //         if (imageUri.HasValue)
            //             imageControl.Image = imageUri.Value;
            //         else
            //             imageControl.ImageUri = imageString;
            //         return imageControl;
            //     });
            // }
            // else if (text != null)
            // {
            //     btn.ContentTemplate = new FuncDataTemplate(_ => true, (_, _) => new TextBlock() { Text = text, VerticalAlignment = VerticalAlignment.Center, Name = "PART_Text"});
            // }
        }
    }
}