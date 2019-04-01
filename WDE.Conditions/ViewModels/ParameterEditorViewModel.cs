using Prism.Commands;
using Prism.Mvvm;

using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Conditions.Data;

using WDE.Conditions.Model;

namespace WDE.Conditions.ViewModels
{
    public class ParameterEditorViewModel : BindableBase
    {
        private readonly IItemFromListProvider itemFromListProvider;

        private bool readOnly;
        private int conditionValueIndex;
        private Condition source;

        public Parameter Parameter { get; set; }

        public DelegateCommand SelectItemAction { get; set; }

        public bool IsReadOnly => readOnly;

        public ParameterEditorViewModel(ConditionParameterJsonData param, IItemFromListProvider itemFromListProvider, IParameterFactory parameterFactory,
            Condition cond, int paramIndex)
        {
            this.itemFromListProvider = itemFromListProvider;
            source = cond;
            conditionValueIndex = paramIndex;

            Parameter = parameterFactory.Factory(param.Type, param.Name);
            Parameter.Items = param.Values;
            Parameter.Description = param.Description;
            Parameter.Value = GetParamValue();

            readOnly = Parameter.Items != null && Parameter.Items.Count > 0;

            SelectItemAction = new DelegateCommand(SelectItem);
            
            Parameter.OnValueChanged += Parameter_OnValueChanged;
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

        private void Parameter_OnValueChanged(object sender, ParameterChangedValue<int> e)
        {
            switch (conditionValueIndex)
            {
                case 0:
                    source.ConditionValueOne = e.New;
                    break;
                case 1:
                    source.ConditionValueTwo = e.New;
                    break;
                case 2:
                    source.ConditionValueThree = e.New;
                    break;
            }
        }

        private int GetParamValue()
        {
            int val = 0;
            switch (conditionValueIndex)
            {
                case 0:
                    val = source.ConditionValueOne;
                    break;
                case 1:
                    val = source.ConditionValueTwo;
                    break;
                case 2:
                    val = source.ConditionValueThree;
                    break;
            }

            return val;
        }
    }
}
