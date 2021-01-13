using System.Windows;
using System.Windows.Input;
using AvalonDock;
using MahApps.Metro.Controls;

namespace WoWDatabaseEditor.Views
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