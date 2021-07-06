using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SourceCodeIntegrationEditor.Views
{
    public class SourceCodeWizardPage : UserControl
    {
        public SourceCodeWizardPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}