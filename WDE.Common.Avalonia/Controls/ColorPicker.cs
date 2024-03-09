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
using WDE.Common.Disposables;
using WDE.Common.Utils;
using HslColor = AvaloniaStyles.Utils.HslColor;

namespace WDE.Common.Avalonia.Controls;

public class WdeColorPicker : Control
{
    private Bitmap? bitmap;
    private int requestVersion = 0;
    private int currentVersion = -1;
    private int currentCalculateVersion = -1;
    private CancellationTokenSource? currentTaskSource;

    public static readonly StyledProperty<double> LightnessProperty = AvaloniaProperty.Register<WdeColorPicker, double>(nameof(Lightness), defaultValue: 0.5f);

    public double Lightness
    {
        get => GetValue(LightnessProperty);
        set => SetValue(LightnessProperty, value);
    }

    public static readonly StyledProperty<double> SelectedHueProperty = AvaloniaProperty.Register<WdeColorPicker, double>(nameof(SelectedHue));

    public double SelectedHue
    {
        get => GetValue(SelectedHueProperty);
        set => SetValue(SelectedHueProperty, value);
    }

    public static readonly StyledProperty<double> SelectedSaturationProperty = AvaloniaProperty.Register<WdeColorPicker, double>(nameof(SelectedSaturation));
    public static readonly DirectProperty<WdeColorPicker, HslColor> SelectedColorProperty = AvaloniaProperty.RegisterDirect<WdeColorPicker, HslColor>("SelectedColor", o => o.SelectedColor, (o, v) => o.SelectedColor = v, default, BindingMode.TwoWay);
    public static readonly StyledProperty<double> HueOffsetProperty = AvaloniaProperty.Register<WdeColorPicker, double>("HueOffset");
    public static readonly DirectProperty<WdeColorPicker, Color> RgbColorProperty = AvaloniaProperty.RegisterDirect<WdeColorPicker, Color>(nameof(RgbColor), o => o.RgbColor);

    public Color RgbColor
    {
        get => SelectedColor.ToRgba();
    }
    
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
            using var _ = SuspendSelectedColorNotification();
            SetCurrentValue(SelectedHueProperty, value.H);
            SetCurrentValue(SelectedSaturationProperty, value.S);
            SetCurrentValue(LightnessProperty, value.L);
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
            using var _ = SuspendSelectedColorNotification();
            SetCurrentValue(SelectedHueProperty, pos.X / Bounds.Width);
            SetCurrentValue(SelectedSaturationProperty, 1 - pos.Y / Bounds.Height);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var pos = e.GetPosition(this);
            using var _ = SuspendSelectedColorNotification();
            SetCurrentValue(SelectedHueProperty, pos.X / Bounds.Width);
            SetCurrentValue(SelectedSaturationProperty, 1 - pos.Y / Bounds.Height);
        }
    }

    static WdeColorPicker()
    {
        ClipToBoundsProperty.OverrideDefaultValue<WdeColorPicker>(true);
        LightnessProperty.Changed.AddClassHandler<WdeColorPicker>((picker, e) =>
        {
            picker.requestVersion++;
            picker.InvalidateVisual();
            if (picker.isSuspended)
                return;
            picker.RaisePropertyChanged(SelectedColorProperty, default, picker.SelectedColor);
            picker.RaisePropertyChanged(RgbColorProperty, default, picker.RgbColor);
        });
        SelectedHueProperty.Changed.AddClassHandler<WdeColorPicker>((picker, e) =>
        {
            if (picker.isSuspended)
                return;
            picker.RaisePropertyChanged(SelectedColorProperty, default, picker.SelectedColor);
            picker.RaisePropertyChanged(RgbColorProperty, default, picker.RgbColor);
        });
        SelectedSaturationProperty.Changed.AddClassHandler<WdeColorPicker>((picker, e) =>
        {
            if (picker.isSuspended)
                return;
            picker.RaisePropertyChanged(SelectedColorProperty, default, picker.SelectedColor);
            picker.RaisePropertyChanged(RgbColorProperty, default, picker.RgbColor);
        });
        AffectsRender<WdeColorPicker>(SelectedHueProperty);
        AffectsRender<WdeColorPicker>(SelectedSaturationProperty);
    }

    private bool isSuspended = false;
    private System.IDisposable SuspendSelectedColorNotification()
    {
        isSuspended = true;
        return new ActionDisposable(() =>
        {
            isSuspended = false;
            RaisePropertyChanged(SelectedColorProperty, default, SelectedColor);
            RaisePropertyChanged(RgbColorProperty, default, RgbColor);
        });
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
