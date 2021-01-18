using System;
using System.Windows;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class WindowManager : IWindowManager
    {
        private readonly Lazy<MainWindow> mainWindow;

        public WindowManager(Lazy<MainWindow> mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        
        public bool ShowDialog(IDialog viewModel)
        {
            DialogWindow view = new DialogWindow();
            view.Height = viewModel.DesiredHeight;
            view.Width = viewModel.DesiredWidth;
            view.Owner = mainWindow.Value;
            view.DataContext = viewModel;
            return view.ShowDialog() ?? false;
        }
    }
}