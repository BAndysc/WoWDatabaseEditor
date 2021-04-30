using System.Windows.Controls;
using System.Windows.Input;
using Prism.Commands;

namespace WDE.DatabaseEditors.WPF.Views.Template
{
    public partial class TemplateDbTableEditorView : UserControl
    {
        public TemplateDbTableEditorView()
        {
            InitializeComponent();
            InputBindings.Add(new KeyBinding(new DelegateCommand(() =>
            {
                TextBox tb = FindName("SearchTextBox") as TextBox;
                tb?.Focus();
            }), new KeyGesture(Key.F, ModifierKeys.Control)));
        }
    }
}