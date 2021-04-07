using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Views;

namespace WoWDatabaseEditorCore.Avalonia.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class WindowManager : IWindowManager
    {
        private readonly IMainWindowHolder mainWindowHolder;

        public WindowManager(IMainWindowHolder mainWindowHolder)
        {
            this.mainWindowHolder = mainWindowHolder;
        }
        
        public async Task<bool> ShowDialog(IDialog viewModel)
        {
            DialogWindow view = new DialogWindow();
            view.Height = viewModel.DesiredHeight;
            view.Width = viewModel.DesiredWidth;
            view.DataContext = viewModel;
            return await view.ShowDialog<bool>(mainWindowHolder.Window);
        }

        public Task<string> ShowFolderPickerDialog(string defaultDirectory)
        {
            return new OpenFolderDialog() {Directory = defaultDirectory}.ShowAsync(mainWindowHolder.Window);
        }
    }
}