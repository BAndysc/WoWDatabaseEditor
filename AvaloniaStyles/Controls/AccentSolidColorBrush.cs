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

     public static readonly StyledProperty<Vector> LProperty =
         AvaloniaProperty.RegisterAttached<Accent, SolidColorBrush, Vector>("L", new Vector(0, 1));

     public static readonly StyledProperty<HslDiff> HueProperty =
         AvaloniaProperty.RegisterAttached<Accent, SolidColorBrush, HslDiff>("Hue");

     static Accent()
     {
         LProperty.Changed.AddClassHandler<SolidColorBrush>(Recolor);
         HueProperty.Changed.AddClassHandler<SolidColorBrush>(Recolor);
     }

     private static void Recolor(SolidColorBrush brush, AvaloniaPropertyChangedEventArgs e)
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

         var l = GetL(brush);
         var hsl = HslColor.FromRgba(baseColor).Scale(e.NewValue as HslDiff, l.X, l.Y);
         brush.Color = hsl.ToRgba();
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

     public static Vector GetL(AvaloniaObject obj)
     {
         return obj.GetValue(LProperty);
     }

     public static void SetL(AvaloniaObject obj, Vector value)
     {
         obj.SetValue(LProperty, value);
     }
}