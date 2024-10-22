using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles;
using Classic.CommonControls;
using Microsoft.Extensions.FileProviders;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace WDE.Common.Avalonia.Components
{
    public class WdeImage : IconRenderer
    {
        // we are never releasing those intentionally
        private static Dictionary<ImageUri, IImage?> cache = new();
        private static Dictionary<ImageUri, Task<IImage?>> loadingTasks = new();
        private static IRuntimeDataService dataAccess;
        private static TaskCompletionSource? loadingFilesListing;
        private static HashSet<string>? existingFiles;
        private static IDirectoryWatcher? watcher;
        private static HashSet<WdeImage> imagesChanged = new();

        public static readonly AttachedProperty<object?> MenuIconProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>("MenuIcon", typeof(WdeImage));

        static WdeImage()
        {
            AffectsRender<WdeImage>(ImageProperty);
            AffectsMeasure<WdeImage>(ImageProperty);
            MenuIconProperty.Changed.AddClassHandler<MenuItem>(OnMenuIconChanged);

            if (Design.IsDesignMode)
            {
                dataAccess = null!;
                return;
            }

            dataAccess = ViewBind.ResolveViewModel<IRuntimeDataService>();
            watcher = dataAccess.WatchDirectory("Icons", true);
            watcher.OnChanged += (type, path) =>
            {
                existingFiles = null;
                var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, path);
                Dispatcher.UIThread.Post(() =>
                {
                    var uri = new ImageUri(relativePath);
                    cache.Remove(new ImageUri(relativePath));
                    foreach (var img in imagesChanged)
                    {
                        img.ImageChanged(uri);
                    }
                });
            };

            ImageUriProperty.Changed.AddClassHandler<WdeImage>((image, e) =>
            {
                if (image.ImageUri == null)
                    return;

                var uri = image.ImageUri;

                async Task Func()
                {
                    if (image.ImageUri == uri)
                    {
                        var bitmap = await LoadBitmapAsync(new ImageUri(image.ImageUri));
                        if (image.ImageUri == uri && bitmap != null)
                        {
                            image.SetCurrentValue(SourceProperty, bitmap);
                        }
                        else if (bitmap == null)
                        {
                            image.ClearValue(SourceProperty);
                        }
                    }
                }

                Func().ListenErrors();
            });
            ImageProperty.Changed.AddClassHandler<WdeImage>((image, e) =>
            {
                var img = image.Image;

                async Task Func()
                {
                    var bitmap = await LoadBitmapAsync(image.Image);
                    if (bitmap != null && image.Image == img)
                    {
                        image.SetCurrentValue(SourceProperty, bitmap);
                    }
                    else if (bitmap == null)
                    {
                        image.ClearValue(SourceProperty);
                    }
                }

                Func().ListenErrors();
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

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            imagesChanged.Add(this);
        }

        private void ImageChanged(ImageUri? triggering)
        {
            if (ImageUri != null)
            {
                if (Source == null || !triggering.HasValue || ImageUri == triggering?.Uri)
                {
                    var uri = ImageUri;
                    SetCurrentValue(ImageUriProperty, null);
                    SetCurrentValue(ImageUriProperty, uri);  // trigger reload
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            imagesChanged.Remove(this);
        }

        private static async Task<bool> FileExists(string? uri)
        {
            if (uri == null)
                return false;

            if (loadingFilesListing != null)
                await loadingFilesListing.Task;

            if (existingFiles == null)
            {
                loadingFilesListing = new TaskCompletionSource();
                var files = (await dataAccess.GetAllFiles("Icons/", "*.png"))
                    .Concat(await dataAccess.GetAllFiles("Icons/", "*.gif"));
                existingFiles = new HashSet<string>();
                foreach (var file in files)
                    existingFiles.Add(file);
                loadingFilesListing.SetResult();
                loadingFilesListing = null;
            }

            return existingFiles.Contains(uri);
        }

        public static async Task<IImage?> LoadBitmapAsync(ImageUri img)
        {
            IImage? bitmap = null;
            if (img.Uri != null && img.Uri.StartsWith("Spell/"))
            {
                if (uint.TryParse(img.Uri.Substring(6), out var id))
                    bitmap = cache[img] = await ViewBind.ResolveViewModel<ISpellIconDatabase>().GetIcon(id);
                else
                    bitmap = cache[img] = null;
            }
            else if (!cache.TryGetValue(img, out bitmap))
            {
                bitmap = cache[img] = await LoadBitmapImplAsync(dataAccess, img);
            }

            return bitmap;
        }

        public static IImage? LoadBitmapNowOrAsync(ImageUri img, Action<IImage?> onLoaded)
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

        public static IImage? LoadBitmapNowOrAsync(ImageUri img, Action onLoaded)
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

        private static async Task<IImage?> LoadBitmapImplAsync(IRuntimeDataService dataService, ImageUri img)
        {
            if (!await FileExists(img.Uri))
                return null;

            if (loadingTasks.TryGetValue(img, out var existingLoadTask))
                return await existingLoadTask;

            var taskCompletionSource = new TaskCompletionSource<IImage?>();
            loadingTasks[img] = taskCompletionSource.Task;

            try
            {
                string? uri = await ResolveUriAsync(img);
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

        private static async Task<string?> ResolveUriAsync(ImageUri image)
        {
            string? uri = image.Uri;
            if (string.IsNullOrEmpty(uri))
                return null;

            if (SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x)
            {
                var win9xUri = Path.ChangeExtension(uri.Replace("Icons/", "Icons/win9x/"), "gif");
                if (await FileExists(win9xUri))
                    uri = win9xUri;
            }

            if (SystemTheme.EffectiveThemeIsDark)
            {
                var extension = Path.GetExtension(uri);
                var darkUri = Path.ChangeExtension(uri, null) + "_dark" + extension;
                if (await FileExists(darkUri))
                    uri = darkUri;
                else
                {
                    var alternativeUri = uri.Replace("_big", "_dark_big");
                    if (await FileExists(alternativeUri))
                        uri = alternativeUri;
                }
            }
            
            if (GlobalApplication.HighDpi)
            {
                var extension = Path.GetExtension(uri);
                var hdpiUri = Path.ChangeExtension(uri, null) + "@2x" + extension;
                if (await FileExists(hdpiUri))
                    uri = hdpiUri;
            }

            if (string.IsNullOrEmpty(uri))
                return null;

            if (!await FileExists(uri))
                return null;
            
            return uri;
        }

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
