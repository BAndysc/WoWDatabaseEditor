using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AvalonDock;
using MahApps.Metro.Controls;
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

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private bool realClosing = false;
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (!realClosing && DataContext is ICloseAwareViewModel closeAwareViewModel)
            {
                TryClose(closeAwareViewModel);
                e.Cancel = !realClosing;
            }
            
            if (!e.Cancel)
                LayoutUtility.SaveLayout(DockingManager);
        }

        private async Task TryClose(ICloseAwareViewModel closeAwareViewModel)
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
                LayoutUtility.LoadLayout(DockingManager, layoutResolver);   
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
        
        /*public void LoadLayout(Stream stream, Action<ITool> addToolCallback, Action<IDocument> addDocumentCallback,
            Dictionary<string, ILayoutItem> itemsState)
        {
            LayoutUtility.LoadLayout(DockingManager, stream, addDocumentCallback, addToolCallback, itemsState);
        }*/

    }
}