using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class MainApplication : IApplication
    {
        private readonly Lazy<MainWindowViewModel> mainWindow;

        public MainApplication(Lazy<MainWindowViewModel> mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public Task<bool> CanClose() => mainWindow.Value.CanClose();

        public Task<bool> TryClose() => mainWindow.Value.TryClose();

        public void ForceClose() => mainWindow.Value.ForceClose();
    }
}