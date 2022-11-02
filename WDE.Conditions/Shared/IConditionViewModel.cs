using WDE.Parameters.Models;

namespace WDE.Conditions.Shared;

public interface IConditionViewModel
{
    ParameterValueHolder<long> GetParameter(int i);
    ParameterValueHolder<long> Invert { get; }
    ParameterValueHolder<long> ConditionValue1 { get; }
    ParameterValueHolder<long> ConditionValue2 { get; }
    ParameterValueHolder<long> ConditionValue3 { get; }
    ParameterValueHolder<long> ConditionTarget { get; }
}