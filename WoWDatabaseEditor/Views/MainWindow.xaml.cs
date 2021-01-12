using System;
using System.Windows;
using System.Windows.Input;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
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

        private void Window_Closed(object sender, EventArgs e)
        {
            XmlLayoutSerializer? layoutSerializer = new(DockingManager);
            layoutSerializer.Serialize(@".\AvalonDock.Layout.config");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLayout();
        }

        public void LoadLayout()
        {
            XmlLayoutSerializer? layoutSerializer = new(DockingManager);
            layoutSerializer.LayoutSerializationCallback += (s, e) =>
            {
                LayoutContent? x = e.Model;
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