using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;

namespace AvaloniaStyles.Demo
{
    public partial class MessageBox : BaseMessageBoxWindow
    {
        public MessageBox()
        {
            InitializeComponent();
        }
    
        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}