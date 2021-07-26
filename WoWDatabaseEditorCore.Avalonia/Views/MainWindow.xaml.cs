using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using AvaloniaStyles.Controls;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public partial class MainWindow : ExtendedWindow
    {
        private ToolsTabControl? tools;

        public MainWindow()
        {
            throw new Exception("you can't call this");
        }
        
        public MainWindow(IMainWindowHolder mainWindowHolder)
        {
            // we have to do it before InitializeComponent!
            var primaryScreen = Screens.Primary ?? Screens.All.FirstOrDefault();
            GlobalApplication.HighDpi = (primaryScreen?.PixelDensity ?? 1) > 1.5f;
            InitializeComponent();
            mainWindowHolder.Window = this;
            this.AttachDevTools();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            tools = this.FindControl<ToolsTabControl>("Tools");
            tools.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is ICloseAwareViewModel closeAwareViewModel)
            {
                closeAwareViewModel.CloseRequest += Close;
                closeAwareViewModel.ForceCloseRequest += () =>
                {
                    realClosing = true;
                    Close();
                };
            }
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
            if (!realClosing && DataContext is MainWindowViewModel closeAwareViewModel)
            {
                TryClose(closeAwareViewModel);
                e.Cancel = true;
            }
        }

        private async Task TryClose(MainWindowViewModel closeAwareViewModel)
        {
            if (await closeAwareViewModel.CanClose())
            {
                realClosing = true;
                Close();
            }
        }
    }
}