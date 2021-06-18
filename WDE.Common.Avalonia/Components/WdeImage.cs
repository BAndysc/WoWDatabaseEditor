using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WDE.Common.Tasks;
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

        public string? ImageUri
        {
            get => Image.Uri;
            set
            {
                if (value != null)
                    Image = new ImageUri(value);
                var old = Image.Uri;
                RaisePropertyChanged(ImageUriProperty, old, value);
            }
        }
        public static readonly DirectProperty<WdeImage, string?> ImageUriProperty = 
            AvaloniaProperty.RegisterDirect<WdeImage, string?>("ImageUri", o => o.ImageUri, (o, v) => o.ImageUri = v);

        private static ImageUri OnSourceChanged(IAvaloniaObject d, ImageUri img)
        {
            if (!cache.TryGetValue(img, out var bitmap))
            {
                bitmap = cache[img] = LoadBitmap(img);
            }
            d.SetValue(SourceProperty, bitmap);

            return img;
        }

        private static Bitmap LoadBitmap(ImageUri img)
        {
            string uri = img.Uri;
            if (GlobalApplication.HighDpi)
            {
                var extension = Path.GetExtension(uri);
                var hdpiUri = Path.ChangeExtension(uri, null) + "@2x" + extension;
                if (File.Exists(hdpiUri))
                    uri = hdpiUri;
            }
            
            return new Bitmap(System.IO.Path.Combine(Environment.CurrentDirectory, uri));
        }
    }
}