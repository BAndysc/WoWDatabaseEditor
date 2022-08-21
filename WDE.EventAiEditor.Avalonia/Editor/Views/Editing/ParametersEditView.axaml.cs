using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.EventAiEditor.Avalonia.Editor.Views.Editing
{
    /// <summary>
    ///     Interaction logic for ParametersEditView.xaml
    /// </summary>
    public partial class ParametersEditView : UserControl
    {
        public ParametersEditView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}