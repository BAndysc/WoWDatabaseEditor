using System;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    [AutoRegister]
    public class NewItemService : INewItemService
    {
        private readonly Func<INewItemDialogViewModel> viewModel;
        private readonly IWindowManager windowManager;

        public NewItemService(Func<INewItemDialogViewModel> newItemWindowViewModel, IWindowManager windowManager)
        {
            viewModel = newItemWindowViewModel;
            this.windowManager = windowManager;
        }

        public async Task<ISolutionItem?> GetNewSolutionItem()
        {
            var vm = viewModel();
            if (await windowManager.ShowDialog(vm))
            {
                return await vm.CreateSolutionItem();
            }
            return null;
        }
    }
}