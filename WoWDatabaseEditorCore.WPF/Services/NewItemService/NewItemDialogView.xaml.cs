using System.Windows.Controls;
using System.Windows.Input;
using WoWDatabaseEditorCore.Services.NewItemService;

namespace WoWDatabaseEditorCore.WPF.Services.NewItemService
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

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && DataContext is NewItemDialogViewModel vm)
                vm.Accept.Execute(null);
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is NewItemDialogViewModel vm)
                vm.Accept.Execute(null);
        }
    }
}