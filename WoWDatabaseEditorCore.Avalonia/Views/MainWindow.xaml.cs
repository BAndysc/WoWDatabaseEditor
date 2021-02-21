using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using WoWDatabaseEditorCore.Avalonia.Controls;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class MainWindow : ExtendedWindow
    {
        public MainWindow()
        {
            ExtendClientAreaChromeHints =
                ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.SystemChrome; 
            
            InitializeComponent();
            this.AttachDevTools();
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