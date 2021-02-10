using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

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
    }
}