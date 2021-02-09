using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.WPF.Views;

namespace WoWDatabaseEditorCore.WPF.Managers
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
        
        public Task<bool> ShowDialog(IDialog viewModel)
        {
            DialogWindow view = new DialogWindow();
            view.Height = viewModel.DesiredHeight;
            view.Width = viewModel.DesiredWidth;
            view.Owner = mainWindow.Value;
            view.DataContext = viewModel;
            return Task.FromResult(view.ShowDialog() ?? false);
        }
    }
}