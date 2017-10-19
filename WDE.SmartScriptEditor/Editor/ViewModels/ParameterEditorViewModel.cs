using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using WDE.Common.Parameters;
using WDE.Common.Providers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParameterEditorViewModel : BindableBase
    {
        private readonly IUnityContainer _container;

        public Parameter Parameter { get; set; }

        public string Group { get; }

        public DelegateCommand SelectItemAction { get; set; }

        public ParameterEditorViewModel(Parameter parameter, string group, IUnityContainer container)
        {
            _container = container;
            Group = group;
            Parameter = parameter;
            SelectItemAction = new DelegateCommand(SelectItem);
        }

        private void SelectItem()
        {
            if (Parameter.Items != null)
            {
                int? val = _container.Resolve<IItemFromListProvider>().GetItemFromList(Parameter.Items, Parameter is FlagParameter);
                if (val.HasValue)
                    Parameter.Value = val.Value;
            }
        }
    }
}
