using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using WoWDatabaseEditor.ViewModels;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

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

        private void Window_Closed(object sender, System.EventArgs e)
        {
            var layoutSerializer = new XmlLayoutSerializer(DockingManager);
            layoutSerializer.Serialize(@".\AvalonDock.Layout.config");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLayout();
        }

        public void LoadLayout()
        {
            var layoutSerializer = new XmlLayoutSerializer(DockingManager);
            layoutSerializer.LayoutSerializationCallback += (s, e) =>
            {
                var x = e.Model;
                e.Cancel = true;
            };
            try
            {
                layoutSerializer.Deserialize(@".\AvalonDock.Layout.config");
            }
            catch (Exception)
            {
            }
        }
    }
}
