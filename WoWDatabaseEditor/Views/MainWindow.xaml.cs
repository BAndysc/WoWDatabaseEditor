using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AvalonDock;
using MahApps.Metro.Controls;
using WDE.Common.Managers;
using WoWDatabaseEditor.Utils;
using WoWDatabaseEditor.ViewModels;

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
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is ICloseAwareViewModel closeAwareViewModel)
            {
                if (!closeAwareViewModel.CanClose())
                    e.Cancel = true;
            }
            
            if (!e.Cancel)
                LayoutUtility.SaveLayout(DockingManager);
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