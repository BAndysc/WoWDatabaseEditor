using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.Conditions.Views;
using WDE.Conditions.ViewModels;

using WDE.Conditions.Model;
using WDE.Conditions.Data;
using WDE.Common.Providers;
using WDE.Common.Parameters;

namespace WDE.Conditions.Providers
{
    [SingleInstance, AutoRegister]
    public class ConditionsEditViewProvider : IConditionsEditViewProvider
    {
        private readonly IConditionDataManager conditionDataManager;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;

        public ConditionsEditViewProvider(IConditionDataManager conditionDataManager, IItemFromListProvider itemFromListProvider, IParameterFactory parameterFactory)
        {
            this.conditionDataManager = conditionDataManager;
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
        }

        public void OpenWindow(ObservableCollection<Condition> conditions)
        {
            ConditionsEditViewModel viewModel = new ConditionsEditViewModel(conditions, conditionDataManager, itemFromListProvider, parameterFactory);
            ConditionsEditView view = new ConditionsEditView();
            view.DataContext = viewModel;
            view.ShowDialog();
        }
    }
}
