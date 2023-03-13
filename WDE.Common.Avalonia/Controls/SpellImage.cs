using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public class SpellImage : Control
{
    private static ISpellIconDatabase iconsDatabase;
    
    public static readonly StyledProperty<uint> SpellIdProperty = AvaloniaProperty.Register<SpellImage, uint>(nameof(SpellId));

    public uint SpellId
    {
        get => GetValue(SpellIdProperty);
        set => SetValue(SpellIdProperty, value);
    }
    
    static SpellImage()
    {
        iconsDatabase = ViewBind.ResolveViewModel<ISpellIconDatabase>();
        SpellIdProperty.Changed.AddClassHandler<SpellImage>((image, e) =>
        {
            image.UpdateBitmapAsync().ListenErrors();
        });
    }

    private IImage? currentBitmap;
    private CancellationTokenSource? currentTask;
    
    private async Task UpdateBitmapAsync()
    {
        currentBitmap = null;
        InvalidateVisual();
        
        if (currentTask != null)
        {
            currentTask.Cancel();
            currentTask = null;
        }

        if (iconsDatabase.TryGetCached(SpellId, out currentBitmap))
        {
            InvalidateVisual();
            return;
        }
        
        var thisTask = new CancellationTokenSource();
        currentTask = thisTask;

        // a small delay to prevent too many requests while scrolling
        await Task.Delay(100, thisTask.Token);
        
        var image = await iconsDatabase.GetIcon(SpellId, thisTask.Token);
        currentBitmap = image;

        if (thisTask.IsCancellationRequested)
            return;

        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(size))
            return new Size(1024, 1024);
        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (currentBitmap != null)
            context.DrawImage(currentBitmap, new Rect(0, 0, size, size));
    }
}