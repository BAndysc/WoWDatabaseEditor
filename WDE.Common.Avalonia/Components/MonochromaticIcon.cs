using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Components
{
    public class MonochromaticIcon : Image
    {
        public static readonly StyledProperty<IBrush> BorderBrushProperty =
            AvaloniaProperty.Register<MonochromaticIcon, IBrush>(nameof(BorderBrush));
        
        public IBrush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }
        
        public static readonly StyledProperty<IBrush> FillProperty =
            AvaloniaProperty.Register<MonochromaticIcon, IBrush>(nameof(Fill));
        
        public IBrush Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly StyledProperty<IBrush> AlternativeBorderBrushProperty =
            AvaloniaProperty.Register<MonochromaticIcon, IBrush>(nameof(AlternativeBorderBrush));
        
        public IBrush AlternativeBorderBrush
        {
            get => GetValue(AlternativeBorderBrushProperty);
            set => SetValue(AlternativeBorderBrushProperty, value);
        }
        
        public static readonly StyledProperty<IBrush> AlternativeBorderBrush2Property =
            AvaloniaProperty.Register<MonochromaticIcon, IBrush>(nameof(AlternativeBorderBrush2));
        
        public IBrush AlternativeBorderBrush2
        {
            get => GetValue(AlternativeBorderBrush2Property);
            set => SetValue(AlternativeBorderBrush2Property, value);
        }
        
        static MonochromaticIcon()
        {
            AffectsRender<MonochromaticIcon>(BorderBrushProperty, FillProperty, AlternativeBorderBrushProperty, AlternativeBorderBrush2Property);
        }
    }
}