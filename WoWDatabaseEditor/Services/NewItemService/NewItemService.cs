using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using WDE.Common;
using Prism.Ioc;

namespace WoWDatabaseEditor.Services.NewItemService
{
    [WDE.Common.Attributes.AutoRegister]
    public class NewItemService : INewItemService
    {
        private readonly Lazy<INewItemWindowViewModel> viewModel;

        public NewItemService(Lazy<INewItemWindowViewModel> newItemWindowViewModel)
        {
            viewModel = newItemWindowViewModel;
        }

        public ISolutionItem GetNewSolutionItem()
        {
            if (new NewItemWindow(viewModel.Value).ShowDialog().Value)
                return viewModel.Value.SelectedPrototype.CreateSolutionItem();
            return null;
        }
    }
}
