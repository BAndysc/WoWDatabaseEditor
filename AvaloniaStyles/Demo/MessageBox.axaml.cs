using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;

namespace AvaloniaStyles.Demo
{
    public class MessageBox : BaseMessageBoxWindow
    {
        public MessageBox()
        {
            InitializeComponent();
        }
    
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}