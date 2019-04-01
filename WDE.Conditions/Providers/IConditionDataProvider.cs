using System;
using System.Collections.Generic;

namespace WDE.Conditions.Data
{
    public interface IConditionDataJsonProvider
    {
        string GetConditionsJson();
        string GetConditionSourcesJson();
    }

    public interface IConditionDataProvider
    {
        IEnumerable<ConditionJsonData> GetConditions();
        IEnumerable<ConditionSourcesJsonData> GetConditionSources();
    }
}
