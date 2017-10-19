using System.Windows;
using System.Windows.Input;
using WoWDatabaseEditor.ViewModels;
using Xceed.Wpf.AvalonDock;

namespace WoWDatabaseEditor.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static DependencyProperty DocumentClosedCommandProperty
        = DependencyProperty.Register(
        "DocumentClosedCommand",
        typeof(ICommand),
        typeof(MainWindow));

        public ICommand DocumentClosedCommand
        {
            get { return (ICommand)GetValue(DocumentClosedCommandProperty); }
            set { SetValue(DocumentClosedCommandProperty, value); }
        }

        private void DockingManager_OnDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            DocumentClosedCommand?.Execute(e.Document.Content);
            if (this.DataContext is MainWindowViewModel)
            {
                (this.DataContext as MainWindowViewModel).DocumentClosed(e.Document.Content);
            }
        }
    }
}
