using Avalonia;
using Avalonia.Media;
using Avalonia.Metadata;
using AvaloniaStyles.Utils;
using HslColor = AvaloniaStyles.Utils.HslColor;

namespace AvaloniaStyles.Controls;

public class Accent
{
     public static readonly StyledProperty<Color> BaseColorProperty =
        AvaloniaProperty.RegisterAttached<Accent, SolidColorBrush, Color>("BaseColor");

     public static readonly StyledProperty<HslDiff> HueProperty =
         AvaloniaProperty.RegisterAttached<Accent, SolidColorBrush, HslDiff>("Hue");

     static Accent()
     {
         HueProperty.Changed.AddClassHandler<SolidColorBrush>((brush, e) =>
         {
             Color baseColor;
             if (!brush.IsSet(BaseColorProperty))
             {
                 baseColor = brush.Color;
                 SetBaseColor(brush, baseColor);
             }
             else
             {
                 baseColor = brush.GetValue(BaseColorProperty);
             }
             var hsl = HslColor.FromRgba(baseColor).Scale(e.NewValue as HslDiff);
             brush.Color = hsl.ToRgba();
         });
     }
     
     public static Color GetBaseColor(AvaloniaObject obj)
     {
         return obj.GetValue(BaseColorProperty);
     }

     public static void SetBaseColor(AvaloniaObject obj, Color value)
     {
         obj.SetValue(BaseColorProperty, value);
     }
    
     public static HslDiff GetHue(AvaloniaObject obj)
     {
         return obj.GetValue(HueProperty);
     }

     public static void SetHue(AvaloniaObject obj, HslDiff value)
     {
         obj.SetValue(HueProperty, value);
     }
}

//
// public sealed class AccentSolidColorBrush : Brush, ISolidColorBrush
//     //, IMutableBrush
// {
//     
//     public static readonly StyledProperty<Color> BaseColorProperty =
//         AvaloniaProperty.Register<AccentSolidColorBrush, Color>(nameof(BaseColor));
//
//     public static readonly StyledProperty<HslDiff> HueProperty =
//         AvaloniaProperty.Register<AccentSolidColorBrush, HslDiff>(nameof(Hue));
//
//     [Content]
//     public Color BaseColor
//     {
//         get => GetValue(BaseColorProperty);
//         set => SetValue(BaseColorProperty, value);
//     }
//
//     public HslDiff Hue
//     {
//         get => GetValue(HueProperty);
//         set => SetValue(HueProperty, value);
//     }
//
//     
//     /// <summary>
//     /// Defines the <see cref="Color"/> property.
//     /// </summary>
//     public static readonly StyledProperty<Color> ColorProperty =
//         AvaloniaProperty.Register<AccentSolidColorBrush, Color>(nameof(Color));
//     
//     /// <summary>
//     /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
//     /// </summary>
//     public AccentSolidColorBrush()
//     {
//     }
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
//     /// </summary>
//     /// <param name="color">The color to use.</param>
//     /// <param name="opacity">The opacity of the brush.</param>
//     public AccentSolidColorBrush(Color color, double opacity = 1)
//     {
//         Color = color;
//         Opacity = opacity;
//     }
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
//     /// </summary>
//     /// <param name="color">The color to use.</param>
//     public AccentSolidColorBrush(uint color)
//         : this(Color.FromUInt32(color))
//     {
//     }
//     
//     /// <summary>
//     /// Gets or sets the color of the brush.
//     /// </summary>
//     public Color Color
//     {
//         get { return GetValue(ColorProperty); }
//         set { SetValue(ColorProperty, value); }
//     }
//
//     /// <summary>
//     /// Parses a brush string.
//     /// </summary>
//     /// <param name="s">The brush string.</param>
//     /// <returns>The <see cref="Color"/>.</returns>
//     /// <remarks>
//     /// Whereas <see cref="Brush.Parse(string)"/> may return an immutable solid color brush,
//     /// this method always returns a mutable <see cref="SolidColorBrush"/>.
//     /// </remarks>
//     public static new SolidColorBrush Parse(string s)
//     {
//         var brush = (ISolidColorBrush)Brush.Parse(s);
//         return brush is SolidColorBrush solid ? solid : new SolidColorBrush(brush.Color);
//     }
//
//     /// <summary>
//     /// Returns a string representation of the brush.
//     /// </summary>
//     /// <returns>A string representation of the brush.</returns>
//     public override string ToString()
//     {
//         return Color.ToString();
//     }
//     //
//     // /// <inheritdoc/>
//     // public IImmutableBrush ToImmutable()
//     // {
//     //     return new ImmutableSolidColorBrush(this);
//     // }
//     //
//     // internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
//     //     static c => new ServerCompositionSimpleSolidColorBrush(c.Server);
//     //
//     // private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
//     // {
//     //     base.SerializeChanges(c, writer);
//     //     ServerCompositionSimpleSolidColorBrush.SerializeAllChanges(writer, Color);
//     // }
// }

// public class AccentSolidColorBrush2 : SolidColorBrush
// {
//     public static readonly StyledProperty<Color> BaseColorProperty =
//         AvaloniaProperty.Register<AccentSolidColorBrush, Color>(nameof(BaseColor));
//
//     public static readonly StyledProperty<HslDiff> HueProperty =
//         AvaloniaProperty.Register<AccentSolidColorBrush, HslDiff>(nameof(Hue));
//
//     private bool inEvent = false;
//     
//     static AccentSolidColorBrush()
//     {
//         BaseColorProperty.Changed.AddClassHandler<AccentSolidColorBrush>((brush, e) =>
//         {
//             var hsl = HslColor.FromRgba(brush.BaseColor).Scale(brush.Hue);
//             brush.inEvent = true;
//             brush.Color = hsl.ToRgba();
//             brush.inEvent = false;
//         });
//         HueProperty.Changed.AddClassHandler<AccentSolidColorBrush>((brush, e) =>
//         {
//             var hsl = HslColor.FromRgba(brush.BaseColor).Scale(brush.Hue);
//             brush.inEvent = true;
//             brush.Color = hsl.ToRgba();
//             brush.inEvent = false;
//         });
//         ColorProperty.Changed.AddClassHandler<AccentSolidColorBrush>((brush, e) =>
//         {
//             if (brush.inEvent)
//                 return;
//             brush.BaseColor = (Color)e.NewValue!;
//         });
//     }
//
//     public AccentSolidColorBrush(Color color, double opacity = 1.0)
//     {
//         BaseColor = color;
//         Opacity = opacity;
//     }
//
//     public AccentSolidColorBrush(uint color) : this(Color.FromUInt32(color))
//     {
//     }
//
//     public AccentSolidColorBrush()
//     {
//         Opacity = 1;
//     }
//
//     /// <summary>Gets or sets the color of the brush.</summary>
//     [Content]
//     public Color BaseColor
//     {
//         get => GetValue(BaseColorProperty);
//         set => SetValue(BaseColorProperty, value);
//     }
//
//     public HslDiff Hue
//     {
//         get => GetValue(HueProperty);
//         set => SetValue(HueProperty, value);
//     }
//
//     /// <summary>Parses a brush string.</summary>
//     /// <param name="s">The brush string.</param>
//     /// <returns>The <see cref="P:Avalonia.Media.SolidColorBrush.Color" />.</returns>
//     /// <remarks>
//     /// Whereas <see cref="M:Avalonia.Media.Brush.Parse(System.String)" /> may return an immutable solid color brush,
//     /// this method always returns a mutable <see cref="T:Avalonia.Media.SolidColorBrush" />.
//     /// </remarks>
//     public static AccentSolidColorBrush Parse(string s)
//     {
//         ISolidColorBrush solidColorBrush1 = (ISolidColorBrush)Brush.Parse(s);
//         return !(solidColorBrush1 is AccentSolidColorBrush solidColorBrush2)
//             ? new AccentSolidColorBrush(solidColorBrush1.Color)
//             : solidColorBrush2;
//     }
//
//     public override string ToString() => Color.ToString();
// }