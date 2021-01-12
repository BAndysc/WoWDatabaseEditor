using System;
using WDE.Common;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services.NewItemService
{
    [AutoRegister]
    public class NewItemService : INewItemService
    {
        private readonly Lazy<INewItemWindowViewModel> viewModel;

        public NewItemService(Lazy<INewItemWindowViewModel> newItemWindowViewModel)
        {
            viewModel = newItemWindowViewModel;
        }

        public ISolutionItem? GetNewSolutionItem()
        {
            if (new NewItemWindow(viewModel.Value).ShowDialog() ?? false)
                return viewModel.Value.SelectedPrototype!.CreateSolutionItem();
            return null;
        }
    }
}