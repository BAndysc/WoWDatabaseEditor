using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;
using WDE.Common.Providers;
using WDE.Conditions.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParametersEditViewModel : BindableBase
    {
        public readonly IConditionDataManager conditionDataManager;
        public readonly IItemFromListProvider itemFromListProvider;
        private readonly SmartBaseElement _element;
        private readonly CollectionViewSource _items;
        public ObservableCollection<ParameterEditorViewModel> Parameters { get; private set; } = new ObservableCollection<ParameterEditorViewModel>();

        public ObservableCollection<Conditions.Model.Condition> _conditions { get; private set; }

        public string Readable => _element.Readable;
        
        public ICollectionView AllItems => _items.View;

        public ParametersEditViewModel(IItemFromListProvider itemFromListProvider, SmartBaseElement element, IEnumerable<KeyValuePair<Parameter, string>> parameters,
            ObservableCollection<Conditions.Model.Condition> conditions, IConditionDataManager conditionDataManager)
        {
            _element = element;
            _conditions = conditions;
            this.conditionDataManager = conditionDataManager;
            this.itemFromListProvider = itemFromListProvider;

            foreach (var parameter in parameters)
            {
                Parameters.Add(new ParameterEditorViewModel(parameter.Key, parameter.Value, itemFromListProvider));
            }

            _items = new CollectionViewSource();
            _items.Source = Parameters;
            _items.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        }
    }
}
