using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.FirstTimeWizard.Views;

public partial class FirstTimeWizardView : UserControl
{
    public FirstTimeWizardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}