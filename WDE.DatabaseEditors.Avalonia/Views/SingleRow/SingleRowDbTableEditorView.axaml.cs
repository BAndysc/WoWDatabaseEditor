using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls.FastTableView;
using WDE.DatabaseEditors.ViewModels.SingleRow;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow
{
    public class SingleRowDbTableEditorView : UserControl
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
    }
}