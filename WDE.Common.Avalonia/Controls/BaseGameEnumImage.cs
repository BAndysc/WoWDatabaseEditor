using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WDE.Common.Avalonia.Components;
using WDE.Common.Types;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public abstract class BaseGameEnumImage : Control
{
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<BaseGameEnumImage, double>("Spacing");

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    static BaseGameEnumImage()
    {
        AffectsRender<BaseGameEnumImage>(SpacingProperty);
        AffectsMeasure<BaseGameEnumImage>(SpacingProperty);
    }

    protected abstract int Value { get; }
    
    protected abstract List<int> EnumValues { get; }
    
    protected abstract List<ImageUri> Images { get; }
    
    protected abstract List<Bitmap?> CachedBitmaps { get; }
    
    protected abstract Task? CacheInProgress { get; set; }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        int icons = 0;
        var enumValue = Value;
        foreach (var icon in EnumValues)
        {
            if ((enumValue & icon) == 0)
                continue;

            icons++;
        }

        var allHeight = availableSize.Height;
        if (double.IsInfinity(allHeight) || double.IsNaN(allHeight))
            allHeight = 1024;
        
        return new Size(icons * allHeight + Math.Max(0, icons - 1) * Spacing, allHeight);
    }

    private bool instanceWaitingForCache = false;
    
    protected bool CacheBitmaps(List<Bitmap?> cache, List<ImageUri> images)
    {
        if (instanceWaitingForCache)
            return false;

        if (CacheInProgress != null)
        {
            instanceWaitingForCache = true;
            async Task InvalidateOnLoad()
            {
                await CacheInProgress!;
                instanceWaitingForCache = false;
                Dispatcher.UIThread.Post(InvalidateVisual);
            }
            InvalidateOnLoad().ListenErrors();
            return false;
        }

        
        if (cache.Count == images.Count)
            return true;
        
        var taskCompletionSource = new TaskCompletionSource();
        CacheInProgress = taskCompletionSource.Task;
        async Task CacheBitmapsAsync()
        {
            cache.Clear();
            foreach (var imageUri in images)
            {
                Bitmap? img = null;
                try
                {
                    img = await WdeImage.LoadBitmapAsync(imageUri);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                cache.Add(img);
            }

            await AdditionalCacheTaskAsync();

            CacheInProgress = null;
            Dispatcher.UIThread.Post(InvalidateVisual);
            taskCompletionSource.SetResult();
        }

        CacheBitmapsAsync().ListenErrors();
        return false;
    }
    
    protected virtual async Task AdditionalCacheTaskAsync() { }
    
    public override void Render(DrawingContext context)
    {
        var enumValue = Value;
        if (enumValue == 0)
            return;
        
        var x = 0d;
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var spacing = Spacing;
        if (!CacheBitmaps(CachedBitmaps, Images))
            return;
      
        for (var index = 0; index < Images.Count; index++)
        {
            var icon = EnumValues[index];
            if ((enumValue & icon) == 0)
                continue;
            
            var rect = new Rect(x, 0, size, size);
            x += size + spacing;

            var bitmap = CachedBitmaps[index];
            if (bitmap == null)
                continue;

            context.DrawImage(bitmap, rect);
        }

        base.Render(context);
    }
}