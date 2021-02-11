using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using WDE.Common.Types;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.Common.Avalonia.Components
{
    public class WdeImage : Image
    {
        // we are never releasing those intentionally
        private static Dictionary<ImageUri, Bitmap> cache = new();

        static WdeImage()
        {
            AffectsRender<WdeImage>(ImageProperty);
            AffectsMeasure<WdeImage>(ImageProperty);
        }
        
        public ImageUri Image
        {
            get => (ImageUri) GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }
        
        public static readonly AvaloniaProperty ImageProperty =
            AvaloniaProperty.Register<WdeImage, ImageUri>(
                nameof(Image),
                coerce: OnSourceChanged);

        private static ImageUri OnSourceChanged(IAvaloniaObject d, ImageUri img)
        {
            if (!cache.TryGetValue(img, out var bitmap))
            {
                bitmap = cache[img] =
                    new Bitmap(System.IO.Path.Combine(Environment.CurrentDirectory, img.Uri));
            }
            d.SetValue(SourceProperty, bitmap);

            return img;
        }
    }
}