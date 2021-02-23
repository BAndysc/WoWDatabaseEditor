using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using WDE.Common.Windows;
using WoWDatabaseEditorCore.Avalonia.Controls;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class MainWindow : ExtendedWindow
    {
        private ToolsTabControl tools;
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
            
            tools = this.FindControl<ToolsTabControl>("Tools");
            tools.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null)
            {
                foreach (var removed in e.RemovedItems)
                {
                    if (removed is ITool tool)
                        tool.Visibility = false;
                }
            }
            if (e.AddedItems != null)
            {
                foreach (var added in e.AddedItems)
                {
                    if (added is ITool tool)
                        tool.Visibility = true;
                }
            }
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