using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

using WDE.Common.Parameters;
using WDE.Common.Providers;
using Prism.Ioc;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParameterEditorViewModel : BindableBase
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public Parameter Parameter { get; set; }

        public string Group { get; }

        public DelegateCommand SelectItemAction { get; set; }

        public ParameterEditorViewModel(Parameter parameter, string group, IItemFromListProvider itemFromListProvider)
        {
            Group = group;
            this.itemFromListProvider = itemFromListProvider;
            Parameter = parameter;
            SelectItemAction = new DelegateCommand(SelectItem);
        }

        private void SelectItem()
        {
            if (Parameter.Items != null)
            {
                int? val = itemFromListProvider.GetItemFromList(Parameter.Items, Parameter is FlagParameter);
                if (val.HasValue)
                    Parameter.Value = val.Value;
            }
        }
    }
}
