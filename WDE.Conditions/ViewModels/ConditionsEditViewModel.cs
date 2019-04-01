using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using WDE.Conditions.Model;
using WDE.Conditions.Data;
using WDE.Common.Providers;
using WDE.Common.Parameters;

namespace WDE.Conditions.ViewModels
{
    public class ConditionsEditViewModel : BindableBase
    {
        private readonly IConditionDataManager conditionDataManager;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;
        private readonly IEnumerable<ConditionJsonData> _conditionTypes;

        private Condition currentElement;
        private ObservableCollection<Condition> _conditions;
        private int condTypeIndex;
        private ObservableCollection<ParameterEditorViewModel> _params;

        public DelegateCommand AddCondition { get; set; }
        public DelegateCommand DeleteCondition { get; set; }

        public ConditionsEditViewModel(ObservableCollection<Condition> conditions, IConditionDataManager conditionDataManager, IItemFromListProvider itemFromListProvider, IParameterFactory parameterFactory)
        {
            _conditions = conditions;
            this.conditionDataManager = conditionDataManager;
            this.itemFromListProvider = itemFromListProvider;
            _conditionTypes = conditionDataManager.GetConditions();
            this.parameterFactory = parameterFactory;

            if (_conditions.Count > 0)
                CurrentElement = _conditions[0];

            AddCondition = new DelegateCommand(AddConditionCommand);
            DeleteCondition = new DelegateCommand(DeleteConditionCommand);
        }

        public ObservableCollection<Condition> Conditions => _conditions;

        public IEnumerable<ConditionJsonData> ConditionTypeItems => _conditionTypes;

        public Condition CurrentElement
        {
            get { return currentElement; }
            set
            {
                currentElement = value;
                CurrentTypeIndex = GetIndexForTypeId(currentElement.ConditionType);
                Parameters = GetParameters(currentElement.ConditionType);
                RaisePropertyChanged("CurrentElement");
            }
        }

        public int CurrentTypeIndex
        {
            get { return condTypeIndex; }
            set
            {
                condTypeIndex = value;
                if (currentElement != null)
                {
                    currentElement.Type = GetTypeIdForIndex(condTypeIndex);
                    Parameters = GetParameters(currentElement.ConditionType);
                }
                
                RaisePropertyChanged("CurrentTypeIndex");
            }
        }

        public ObservableCollection<ParameterEditorViewModel> Parameters
        {
            get { return _params; }
            set
            {
                _params = value;
                RaisePropertyChanged("Parameters");
            }
        }

        private void AddConditionCommand()
        {
            Condition cond = new Condition(conditionDataManager);
            //CurrentElement = cond;
            _conditions.Add(cond);
        }

        private void DeleteConditionCommand()
        {
            if (currentElement != null)
            {
                _conditions.Remove(currentElement);
                CurrentElement = null;
            }
        }

        private int GetTypeIdForIndex(int val)
        {
            List<ConditionJsonData> list = _conditionTypes.ToList();
            return list[condTypeIndex].Id;
        }

        private int GetIndexForTypeId(int type)
        {
            int index = 0;
            List<ConditionJsonData> list = _conditionTypes.ToList();

            foreach (var data in list)
            {
                if (data.Id == type)
                    break;

                    ++index;
            }

            return index;
        }

        private ObservableCollection<ParameterEditorViewModel> GetParameters(int conditionType)
        {
            ObservableCollection<ParameterEditorViewModel> list = new ObservableCollection<ParameterEditorViewModel>();
            ConditionJsonData data = conditionDataManager.GetConditionData(conditionType);

            if (data.Parameters != null)
            {
                int index = 0;
                foreach (var conditionParam in data.Parameters)
                    list.Add(new ParameterEditorViewModel(conditionParam, itemFromListProvider, parameterFactory, currentElement, index++));
            }

            return list;
        }
    }
}
