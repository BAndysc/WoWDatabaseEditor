using System;
using System.Linq;
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
            try
            {
                DialogWindow view = new DialogWindow();
                view.Height = viewModel.DesiredHeight;
                view.Width = viewModel.DesiredWidth;
                view.DataContext = viewModel;
                return await view.ShowDialog<bool>(mainWindowHolder.Window);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public Task<string?> ShowFolderPickerDialog(string defaultDirectory)
        {
            return new OpenFolderDialog() {Directory = defaultDirectory}.ShowAsync(mainWindowHolder.Window);
        }

        public async Task<string?> ShowOpenFileDialog(string filter, string? defaultDirectory = null)
        {
            var f = filter.Split("|");
            
            var result = await new OpenFileDialog()
            {
                Directory = defaultDirectory,
                AllowMultiple = false,
                Filters = f.Zip(f.Skip(1))
                    .Where((x, i) => i % 2 == 0)
                    .Select(x => new FileDialogFilter()
                    {
                        Name = x.First,
                        Extensions = x.Second.Split(",").ToList()
                    }).ToList()
            }.ShowAsync(mainWindowHolder.Window);
            
            if (result == null || result.Length == 0)
                return null;
            
            return result[0];
        }
    }
}