using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AvalonDock;
using MahApps.Metro.Controls;
using WDE.Common.Managers;
using WDE.Common.Services;
using WoWDatabaseEditorCore.ViewModels;
using WoWDatabaseEditorCore.WPF.Utils;

namespace WoWDatabaseEditorCore.WPF.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static DependencyProperty DocumentClosedCommandProperty =
            DependencyProperty.Register("DocumentClosedCommand", typeof(ICommand), typeof(MainWindow));

        public IFileSystem fileSystem;
        
        public MainWindow(IFileSystem fileSystem)
        {
            InitializeComponent();
            this.fileSystem = fileSystem;
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private bool realClosing = false;
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (!realClosing && DataContext is IApplication closeAwareViewModel)
            {
                TryClose(closeAwareViewModel);
                e.Cancel = !realClosing;
            }
            
            if (!e.Cancel)
                LayoutUtility.SaveLayout(DockingManager, fileSystem);
        }

        private async Task TryClose(IApplication closeAwareViewModel)
        {
            if (await closeAwareViewModel.CanClose())
            {
                realClosing = true;
                Close();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ILayoutViewModelResolver layoutResolver)
            {
                LayoutUtility.LoadLayout(DockingManager, layoutResolver, fileSystem);   
            }
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

        public ICommand DocumentClosedCommand
        {
            get => (ICommand) GetValue(DocumentClosedCommandProperty);
            set => SetValue(DocumentClosedCommandProperty, value);
        }

        private void DockingManager_OnDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            DocumentClosedCommand?.Execute(e.Document.Content);
        }
    }
}