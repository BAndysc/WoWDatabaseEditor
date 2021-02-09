using System;
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

        public ISolutionItem? GetNewSolutionItem()
        {
            var vm = viewModel();
            if (windowManager.ShowDialog(vm))
            {
                return vm.SelectedPrototype!.CreateSolutionItem();
            }
            return null;
        }
    }
}