using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaStyles.Utils;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public class ColorPicker : Control
{
    private IBitmap? bitmap;
    private int requestVersion = 0;
    private int currentVersion = -1;
    private int currentCalculateVersion = -1;
    private CancellationTokenSource? currentTaskSource;

    public static readonly StyledProperty<double> LightnessProperty = AvaloniaProperty.Register<ColorPicker, double>(nameof(Lightness));

    public double Lightness
    {
        get => GetValue(LightnessProperty);
        set => SetValue(LightnessProperty, value);
    }

    public static readonly StyledProperty<double> SelectedHueProperty = AvaloniaProperty.Register<ColorPicker, double>(nameof(SelectedHue));

    public double SelectedHue
    {
        get => GetValue(SelectedHueProperty);
        set => SetValue(SelectedHueProperty, value);
    }

    public static readonly StyledProperty<double> SelectedSaturationProperty = AvaloniaProperty.Register<ColorPicker, double>(nameof(SelectedSaturation));
    public static readonly DirectProperty<ColorPicker, HslColor> SelectedColorProperty = AvaloniaProperty.RegisterDirect<ColorPicker, HslColor>("SelectedColor", o => o.SelectedColor, (o, v) => o.SelectedColor = v, default, BindingMode.TwoWay);
    public static readonly StyledProperty<double> HueOffsetProperty = AvaloniaProperty.Register<ColorPicker, double>("HueOffset");

    public double SelectedSaturation
    {
        get => GetValue(SelectedSaturationProperty);
        set => SetValue(SelectedSaturationProperty, value);
    }

    public HslColor SelectedColor
    {
        get => new HslColor(SelectedHue, SelectedSaturation, Lightness);
        set
        {
            SelectedHue = value.H;
            SelectedSaturation = value.S;
            Lightness = value.L;
        }
    }

    public double HueOffset
    {
        get => (double)GetValue(HueOffsetProperty);
        set => SetValue(HueOffsetProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var pos = e.GetPosition(this);
            SelectedHue = pos.X / Bounds.Width;
            SelectedSaturation = 1 - pos.Y / Bounds.Height;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var pos = e.GetPosition(this);
            SelectedHue = pos.X / Bounds.Width;
            SelectedSaturation = 1 - pos.Y / Bounds.Height;
        }
    }

    static ColorPicker()
    {
        ClipToBoundsProperty.OverrideDefaultValue<ColorPicker>(true);
        LightnessProperty.Changed.AddClassHandler<ColorPicker>((picker, e) =>
        {
            picker.requestVersion++;
            picker.InvalidateVisual();
        });
        SelectedHueProperty.Changed.AddClassHandler<ColorPicker>((picker, e) =>
        {
            picker.RaisePropertyChanged(SelectedColorProperty, default, picker.SelectedColor);
        });
        SelectedSaturationProperty.Changed.AddClassHandler<ColorPicker>((picker, e) =>
        {
            picker.RaisePropertyChanged(SelectedColorProperty, default, picker.SelectedColor);
        });
        AffectsRender<ColorPicker>(SelectedHueProperty);
        AffectsRender<ColorPicker>(SelectedSaturationProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (bitmap != null)
        {
            context.DrawImage(bitmap, new Rect(0, 0, Bounds.Width, Bounds.Height));
            
            var x = SelectedHue * Bounds.Width;
            var y = (1 - SelectedSaturation) * Bounds.Height;
            
            context.DrawGeometry(Brushes.Transparent, new Pen(Brushes.White, 1), new EllipseGeometry(new Rect(x-10,y-10, 20, 20)));
        }

        if (requestVersion != currentVersion && currentCalculateVersion != requestVersion || bitmap == null)
        {
            //currentTaskSource?.Cancel();
            currentTaskSource = new CancellationTokenSource();
            AsyncRedraw(currentTaskSource.Token, requestVersion).ListenErrors();
        }
    }
    
    private static WriteableBitmap CreateBitmapFromPixelData(
        byte[] bgraPixelData,
        int pixelWidth,
        int pixelHeight)
    {
        // Standard may need to change on some devices
        Vector dpi = new Vector(96, 96);

        var bitmap = new WriteableBitmap(
            new PixelSize(pixelWidth, pixelHeight),
            dpi,
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using (var frameBuffer = bitmap.Lock())
        {
            Marshal.Copy(bgraPixelData, 0, frameBuffer.Address, pixelHeight * pixelWidth * 4);
        }

        return bitmap;
    }

    private async Task AsyncRedraw(CancellationToken token, int version)
    {
        currentCalculateVersion = version;
        int width = Math.Max(1, (int)Bounds.Width);
        int height = Math.Max(1, (int)Bounds.Height);
        byte[] array = ArrayPool<byte>.Shared.Rent(width * height * 4);
        await ColorPickerUtils.CreateComponentBitmapAsync(token, array, width, height, HueOffset, Lightness);
        if (currentVersion < version && !token.IsCancellationRequested)
        {
            var bitmap = CreateBitmapFromPixelData(array, width, height);
            //this.bitmap?.Dispose();
            this.bitmap = bitmap;
            currentVersion = version;
            InvalidateVisual();
        }
        ArrayPool<byte>.Shared.Return(array);
    }
}