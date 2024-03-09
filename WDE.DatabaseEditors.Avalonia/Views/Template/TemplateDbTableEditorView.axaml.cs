using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Prism.Commands;

namespace WDE.DatabaseEditors.Avalonia.Views.Template
{
    public partial class TemplateDbTableEditorView : UserControl
    {
        public TemplateDbTableEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}