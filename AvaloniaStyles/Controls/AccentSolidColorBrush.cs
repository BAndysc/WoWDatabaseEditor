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