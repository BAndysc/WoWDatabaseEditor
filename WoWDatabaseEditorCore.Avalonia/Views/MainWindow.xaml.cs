using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaTitleBarHeightHint = -1;            

            TransparencyLevelHint = WindowTransparencyLevel.AcrylicBlur;     
            
            InitializeComponent();
            this.AttachDevTools();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
                        
            ExtendClientAreaChromeHints = 
                global::Avalonia.Platform.ExtendClientAreaChromeHints.PreferSystemChrome |                 
                global::Avalonia.Platform.ExtendClientAreaChromeHints.OSXThickTitleBar;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private bool realClosing = false;
        
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!realClosing && DataContext is ICloseAwareViewModel closeAwareViewModel)
            {
                TryClose(closeAwareViewModel);
                e.Cancel = true;
            }
        }

        private async Task TryClose(ICloseAwareViewModel closeAwareViewModel)
        {
            if (await closeAwareViewModel.CanClose())
            {
                realClosing = true;
                Close();
            }
        }
    }
}