using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles;
using Microsoft.Extensions.FileProviders;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.Common.Avalonia.Components
{
    public class WdeImage : Image
    {
        // we are never releasing those intentionally
        private static Dictionary<ImageUri, Bitmap?> cache = new();
        private static Dictionary<ImageUri, Task<Bitmap?>> loadingTasks = new();
        private static IRuntimeDataService dataAccess;

        public static readonly AttachedProperty<object?> MenuIconProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>("MenuIcon", typeof(WdeImage));

        static WdeImage()
        {
            AffectsRender<WdeImage>(ImageProperty);
            AffectsMeasure<WdeImage>(ImageProperty);
            MenuIconProperty.Changed.AddClassHandler<MenuItem>(OnMenuIconChanged);

            dataAccess = ViewBind.ResolveViewModel<IRuntimeDataService>();

            ImageUriProperty.Changed.AddClassHandler<WdeImage>((image, e) =>
            {
                if (image.ImageUri == null)
                    return;

                var uri = image.ImageUri;
                
                //Dispatcher.UIThread.Post(() =>
                {
                    async Task Func()
                    {
                        if (image.ImageUri == uri)
                        {
                            var bitmap = await LoadBitmapAsync(new ImageUri(image.ImageUri));
                            if (image.ImageUri == uri && bitmap != null)
                                image.SetValue(SourceProperty, bitmap);
                            else
                                image.ClearValue(SourceProperty);
                        }
                    }

                    Func().ListenErrors();
                }//);
            });
            ImageProperty.Changed.AddClassHandler<WdeImage>((image, e) =>
            {
                var img = image.Image;
                
                //Dispatcher.UIThread.Post(() =>
                {
                    async Task Func()
                    {
                        var bitmap = await LoadBitmapAsync(image.Image);
                        if (bitmap != null && image.Image == img)
                            image.SetValue(SourceProperty, bitmap); 
                        else
                            image.ClearValue(SourceProperty);
                    }

                    Func().ListenErrors();
                }//);
            });
        }
        
        public ImageUri Image
        {
            get => (ImageUri?) GetValue(ImageProperty) ?? new ImageUri("");
            set => SetValue(ImageProperty, value);
        }

        public static readonly AvaloniaProperty ImageProperty = AvaloniaProperty.Register<WdeImage, ImageUri>(nameof(Image));

        public string? ImageUri
        {
            get => (string?)GetValue(ImageUriProperty);
            set => SetValue(ImageUriProperty, value);
        }
        public static readonly AvaloniaProperty<string?> ImageUriProperty = 
            AvaloniaProperty.Register<WdeImage, string?>(nameof(ImageUri));

        public static async Task<Bitmap?> LoadBitmapAsync(ImageUri img)
        {
            if (!cache.TryGetValue(img, out var bitmap))
            {
                bitmap = cache[img] = await LoadBitmapImplAsync(dataAccess, img);
            }

            return bitmap;
        }

        public static Bitmap? LoadBitmapNowOrAsync(ImageUri img, Action<Bitmap?> onLoaded)
        {
            if (!cache.TryGetValue(img, out var bitmap))
            {
                async Task LoadAsync()
                {
                    var bitmap = await LoadBitmapAsync(img);
                    Dispatcher.UIThread.Post(() => onLoaded(bitmap), DispatcherPriority.Default);
                }
                LoadAsync().ListenErrors();
                return null;
            }

            return bitmap;
        }

        public static Bitmap? LoadBitmapNowOrAsync(ImageUri img, Action onLoaded)
        {
            if (!cache.TryGetValue(img, out var bitmap))
            {
                async Task LoadAsync()
                {
                    await LoadBitmapAsync(img);
                    Dispatcher.UIThread.Post(onLoaded, DispatcherPriority.Default);
                }
                LoadAsync().ListenErrors();
                return null;
            }

            return bitmap;
        }

        private static async Task<Bitmap?> LoadBitmapImplAsync(IRuntimeDataService dataService, ImageUri img)
        {
            if (loadingTasks.TryGetValue(img, out var existingLoadTask))
                return await existingLoadTask;

            var taskCompletionSource = new TaskCompletionSource<Bitmap?>();
            loadingTasks[img] = taskCompletionSource.Task;

            try
            {
                string? uri = await ResolveUriAsync(img, dataService.Exists);
                if (uri == null)
                {
                    taskCompletionSource.SetResult(null);
                    return null;
                }

                var bytes = await dataService.ReadAllBytes(uri);
                var bitmap = new Bitmap(new MemoryStream(bytes));
                taskCompletionSource.SetResult(bitmap);
                return bitmap;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(null);
                throw;
            }
            finally
            {
                loadingTasks.Remove(img);
            }
        }

        private static async Task<string?> ResolveUriAsync(ImageUri image, Func<string, Task<bool>> exists)
        {
            string? uri = image.Uri;
            if (string.IsNullOrEmpty(uri))
                return null;
             
            if (SystemTheme.EffectiveThemeIsDark)
            {
                var extension = Path.GetExtension(uri);
                var darkUri = Path.ChangeExtension(uri, null) + "_dark" + extension;
                if (await exists(darkUri))
                    uri = darkUri;
                else
                {
                    var alternativeUri = uri.Replace("_big", "_dark_big");
                    if (await exists(alternativeUri))
                        uri = alternativeUri;
                }
            }
            
            if (GlobalApplication.HighDpi)
            {
                var extension = Path.GetExtension(uri);
                var hdpiUri = Path.ChangeExtension(uri, null) + "@2x" + extension;
                if (await exists(hdpiUri))
                    uri = hdpiUri;
            }

            if (string.IsNullOrEmpty(uri))
                return null;

            if (!await exists(uri))
                return null;
            
            return uri;
        }
        
        // private static string? ResolveUriSync(ImageUri image, Func<string, bool> exists)
        // {
        //     string? uri = image.Uri;
        //     if (string.IsNullOrEmpty(uri))
        //         return null;
        //
        //     if (SystemTheme.EffectiveThemeIsDark)
        //     {
        //         var extension = Path.GetExtension(uri);
        //         var darkUri = Path.ChangeExtension(uri, null) + "_dark" + extension;
        //         if (exists(darkUri))
        //             uri = darkUri;
        //         else
        //         {
        //             var alternativeUri = uri.Replace("_big", "_dark_big");
        //             if (exists(alternativeUri))
        //                 uri = alternativeUri;
        //         }
        //     }
        //
        //     if (GlobalApplication.HighDpi)
        //     {
        //         var extension = Path.GetExtension(uri);
        //         var hdpiUri = Path.ChangeExtension(uri, null) + "@2x" + extension;
        //         if (exists(hdpiUri))
        //             uri = hdpiUri;
        //     }
        //
        //     if (string.IsNullOrEmpty(uri))
        //         return null;
        //
        //     return uri;
        // }
        //
        // private static Bitmap? LoadBitmapImpl(ImageUri img)
        // {
        //     Console.WriteLine("Synchronous LoadBitmap. It should be avoided. Printing stacktrace for reference: ");
        //     Console.WriteLine(new StackTrace(true));
        //
        //     if (OperatingSystem.IsBrowser()) // no File api
        //         return null;
        //
        //     string? uri = ResolveUriSync(img, File.Exists);
        //     if (uri == null)
        //         return null;
        //
        //     var path = Path.Combine(Environment.CurrentDirectory, uri);
        //
        //     if (!File.Exists(path))
        //         return null;
        //
        //     return new Bitmap(path);
        // }

        public static object? GetMenuIcon(AvaloniaObject obj)
        {
            return (object?)obj.GetValue(MenuIconProperty);
        }

        public static void SetMenuIcon(AvaloniaObject obj, object? value)
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
