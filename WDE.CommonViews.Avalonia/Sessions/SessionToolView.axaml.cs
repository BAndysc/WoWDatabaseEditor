using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WDE.Sessions.Sessions;

namespace WDE.CommonViews.Avalonia.Sessions
{
    public class SessionToolView : UserControl
    {
        public SessionToolView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Tv_OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SessionToolViewModel vm && vm.SelectedItem != null)
                vm.RequestOpenItemCommand.Execute(vm.SelectedItem);
        }
    }
}