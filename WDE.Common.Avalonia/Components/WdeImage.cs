using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using AvaloniaStyles;
using WDE.Common.Tasks;
using WDE.Common.Types;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.Common.Avalonia.Components
{
    public class WdeImage : Image
    {
        // we are never releasing those intentionally
        private static Dictionary<ImageUri, Bitmap?> cache = new();

        public static readonly AttachedProperty<object?> MenuIconProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, object?>("MenuIcon", typeof(WdeImage));

        static WdeImage()
        {
            AffectsRender<WdeImage>(ImageProperty);
            AffectsMeasure<WdeImage>(ImageProperty);
            MenuIconProperty.Changed.AddClassHandler<MenuItem>(OnMenuIconChanged);
        }

        public ImageUri Image
        {
            get => (ImageUri?) GetValue(ImageProperty) ?? default;
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
                else
                    Image = WDE.Common.Types.ImageUri.Empty;
                var old = Image.Uri;
                RaisePropertyChanged(ImageUriProperty, old, value);
            }
        }
        public static readonly DirectProperty<WdeImage, string?> ImageUriProperty = 
            AvaloniaProperty.RegisterDirect<WdeImage, string?>("ImageUri", o => o.ImageUri, (o, v) => o.ImageUri = v);

        private static ImageUri OnSourceChanged(IAvaloniaObject d, ImageUri img)
        {
            var bitmap = LoadBitmap(img);
            if (bitmap != null)
                d.SetValue(SourceProperty, bitmap);
            else
                d.ClearValue(SourceProperty);

            return img;
        }

        public static Bitmap? LoadBitmap(ImageUri img)
        {
            if (!cache.TryGetValue(img, out var bitmap))
            {
                bitmap = cache[img] = LoadBitmapImpl(img);
            }

            return bitmap;
        }

        private static Bitmap? LoadBitmapImpl(ImageUri img)
        {
            if (img.Uri == null)
                return null;
            string uri = img.Uri;
            if (SystemTheme.EffectiveThemeIsDark)
            {
                var extension = Path.GetExtension(uri);
                var darkUri = Path.ChangeExtension(uri, null) + "_dark" + extension;
                if (File.Exists(darkUri))
                    uri = darkUri;
                else
                {
                    var alternativeUri = uri.Replace("_big", "_dark_big");
                    if (File.Exists(alternativeUri))
                        uri = alternativeUri;
                }
            }

            if (GlobalApplication.HighDpi)
            {
                var extension = Path.GetExtension(uri);
                var hdpiUri = Path.ChangeExtension(uri, null) + "@2x" + extension;
                if (File.Exists(hdpiUri))
                    uri = hdpiUri;
            }

            if (string.IsNullOrEmpty(uri))
                return null;

            var path = Path.Combine(Environment.CurrentDirectory, uri);

            if (!File.Exists(path))
                return null;

            return new Bitmap(path);
        }

        public static object? GetMenuIcon(IAvaloniaObject obj)
        {
            return (object?)obj.GetValue(MenuIconProperty);
        }

        public static void SetMenuIcon(IAvaloniaObject obj, object? value)
        {
            obj.SetValue(MenuIconProperty, value);
        }

        private static void OnMenuIconChanged(MenuItem menuItem, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is null)
            {
                menuItem.Icon = null!;
                return;
            }

            WdeImage? image = menuItem.Icon as WdeImage;
            if (image == null)
            {
                image = new WdeImage();
                menuItem.Icon = image;
            }

            if (e.NewValue is ImageUri uri)
            {
                image.Image = uri;
            }
            else if (e.NewValue is string s)
            {
                image.ImageUri = s;
            }
            else
            {
                throw new Exception("Invalid type: MenuIcon must be string? or ImageUri?, got " + e.NewValue.GetType() + " instead");
            }
        }
    }
}