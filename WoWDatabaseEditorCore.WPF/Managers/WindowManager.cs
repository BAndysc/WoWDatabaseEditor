using System;
using System.Threading.Tasks;
using Ookii.Dialogs.Wpf;
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
        
        public Task<string?> ShowFolderPickerDialog(string defaultDirectory)
        {
            VistaFolderBrowserDialog dialog = new();
            dialog.SelectedPath = defaultDirectory;
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
                return Task.FromResult<string?>(dialog.SelectedPath);
            return Task.FromResult<string?>(null);
        }
        
        public Task<string?> ShowOpenFileDialog(string filter, string? defaultDirectory = null)
        {
            VistaOpenFileDialog dialog = new();
            if (!string.IsNullOrEmpty(defaultDirectory))
                dialog.InitialDirectory = defaultDirectory;
            dialog.Multiselect = false;
            dialog.Filter = filter;

            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
                return Task.FromResult<string?>(dialog.FileName);
            return Task.FromResult<string?>(null);
        }

        public Task<string?> ShowSaveFileDialog(string filter, string? defaultDirectory = null)
        {
            throw new NotImplementedException();
        }
        
        public void OpenUrl(string url)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            };
            System.Diagnostics.Process.Start(psi);
        }
    }
}