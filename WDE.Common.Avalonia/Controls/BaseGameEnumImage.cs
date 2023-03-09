using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Components;
using WDE.Common.Types;

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

    protected static void CacheBitmaps(List<Bitmap?> cache, List<ImageUri> images)
    {
        if (cache.Count == images.Count)
            return;
        
        cache.Clear();
        foreach (var imageUri in images)
        {
            cache.Add(WdeImage.LoadBitmap(imageUri));
        }
    }
    
    public override void Render(DrawingContext context)
    {
        var enumValue = Value;
        if (enumValue == 0)
            return;
        
        var x = 0d;
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var spacing = Spacing;
        CacheBitmaps(CachedBitmaps, Images);
        
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