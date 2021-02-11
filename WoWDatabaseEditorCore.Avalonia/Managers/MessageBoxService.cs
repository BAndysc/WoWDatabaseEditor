using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class MessageBoxService : IMessageBoxService
    {
        private readonly Lazy<MainWindow> mainWindow;

        public MessageBoxService(Lazy<MainWindow> mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        
        public async Task<T> ShowDialog<T>(IMessageBox<T> messageBox)
        {
            MessageBoxView view = new MessageBoxView();
            var viewModel = new MessageBoxViewModel<T>(messageBox);
            view.DataContext = viewModel;
            await view.ShowDialog<bool>(mainWindow.Value);
            return viewModel.SelectedOption;
        }
    }
}