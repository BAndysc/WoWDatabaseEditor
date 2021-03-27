using System.Windows;
using System.Windows.Controls;

namespace WDE.SmartScriptEditor.WPF.Editor.UserControls
{
    public class MiniEventIcon : Control
    {
        public static DependencyProperty TextProperty =
                DependencyProperty.Register(nameof(Text), typeof(string), typeof(MiniEventIcon));
        
        static MiniEventIcon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MiniEventIcon), new FrameworkPropertyMetadata(typeof(MiniEventIcon)));
        }
        
        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }
}