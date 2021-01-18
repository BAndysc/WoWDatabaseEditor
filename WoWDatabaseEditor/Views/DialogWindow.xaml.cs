using System.Windows;
using MahApps.Metro.Controls;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.Views
{
    public partial class DialogWindow : MetroWindow
    {
        public DialogWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel -= DialogWindowOnCloseCancel;
                dialogWindow.CloseOk -= DialogWindowOnCloseOk;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IDialog dialogWindow)
            {
                dialogWindow.CloseCancel += DialogWindowOnCloseCancel;
                dialogWindow.CloseOk += DialogWindowOnCloseOk;
            }
        }

        private void DialogWindowOnCloseOk()
        {
            DialogResult = true;
            Close();
        }

        private void DialogWindowOnCloseCancel()
        {
            DialogResult = false;
            Close();
        }
    }
}