using Avalonia;
using Avalonia.Controls;
using AvaloniaStyles.Controls;

namespace WDE.Common.Avalonia.Components
{
    public partial class ToolView : UserControl
    {
        public static readonly StyledProperty<MonochromaticIcon> IconProperty =
            AvaloniaProperty.Register<ToolView, MonochromaticIcon>(nameof(Icon));
        
        public MonochromaticIcon Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}