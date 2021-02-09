using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WDE.Common.Types;

namespace WDE.Common.WPF.Components
{
    public class WdeImage : Image
    {
        // we are never releasing those intentionally
        private static Dictionary<ImageUri, BitmapImage> cache = new();
        
        public ImageUri Image
        {
            get => (ImageUri) GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }
        
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                nameof(Image),
                typeof(ImageUri),
                typeof(WdeImage),
                new FrameworkPropertyMetadata(
                    default(ImageUri),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    new PropertyChangedCallback(OnSourceChanged),
                    null),
                null);

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var img = (ImageUri)d.GetValue(ImageProperty);
            if (!cache.TryGetValue(img, out var bitmap))
            {
                bitmap = cache[img] =
                    new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, img.Uri), UriKind.Absolute));
            }
            d.SetValue(SourceProperty, bitmap);
        }
    }
}