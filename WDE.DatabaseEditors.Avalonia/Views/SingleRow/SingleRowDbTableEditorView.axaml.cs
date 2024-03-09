using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls.FastTableView;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.SingleRow;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow
{
    public partial class SingleRowDbTableEditorView : UserControl
    {
        public SingleRowDbTableEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void VeryFastTableView_OnValueUpdateRequest(string text)
        {
            (DataContext as SingleRowDbTableEditorViewModel)!.UpdateSelectedCells(text);
        }

        private int customContextMenus = 0;

        private void Visual_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            VeryFastTableView tableView = (VeryFastTableView)sender!;
            if (DataContext is not ViewModelBase viewModel)
                return;

            if (viewModel.ContextCommands.Count == 0)
                return;

            var contextMenu = tableView.ContextMenu!;
            var items = contextMenu.ItemsSource as AvaloniaList<object>;
            if (items == null)
                return;

            customContextMenus = 0;
            foreach (var command in viewModel.ContextCommands)
            {
                var menuItem = new MenuItem() { Header = command.Name, Command = command.Command };
                if (command.KeyGesture != null)
                    menuItem.InputGesture = KeyGesture.Parse(command.KeyGesture);
                items.Insert(customContextMenus++, menuItem);
            }
            items.Insert(customContextMenus, new Separator());
        }

        private void Visual_OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            VeryFastTableView tableView = (VeryFastTableView)sender!;
            var contextMenu = tableView.ContextMenu!;
            var items = contextMenu.ItemsSource as AvaloniaList<object>;
            if (items == null)
                return;
            for (int i = 0; i < customContextMenus; ++i)
                items.RemoveAt(0);
            customContextMenus = 0;
        }
    }
}