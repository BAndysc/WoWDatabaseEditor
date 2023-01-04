using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WoWDatabaseEditorCore.Services.NewItemService;

namespace WoWDatabaseEditorCore.Avalonia.Services.NewItemService
{
    /// <summary>
    ///     Interaction logic for NewItemWindow.xaml
    /// </summary>
    public partial class NewItemDialogView : UserControl
    {
        public NewItemDialogView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is NewItemDialogViewModel vm)
                vm.Accept.Execute(null);
        }

        private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is NewItemDialogViewModel vm)
                vm.Accept.Execute(null);
        }
    }
}