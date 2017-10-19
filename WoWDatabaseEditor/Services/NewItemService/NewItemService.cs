using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using WDE.Common;

namespace WoWDatabaseEditor.Services.NewItemService
{
    public class NewItemService : INewItemService
    {
        private IUnityContainer container;

        public NewItemService(IUnityContainer container)
        {
            this.container = container;
        }

        public ISolutionItem GetNewSolutionItem()
        {
            INewItemWindowViewModel viewModel = container.Resolve<INewItemWindowViewModel>();
            if (new NewItemWindow(viewModel).ShowDialog().Value)
                return viewModel.SelectedPrototype.CreateSolutionItem();
            return null;
        }
    }
}
