using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    public enum ConditionDataType
    {
        DataCondition = 1,
        DataConditionSource = 2,
    }

    public interface IConditionDataManager
    {
        ConditionJsonData GetConditionData(int id);
        ConditionJsonData GetConditionData(string name);

        ConditionSourcesJsonData GetConditionSourceData(int id);
        ConditionSourcesJsonData GetConditionSourceData(string name);
    }

    [SingleInstance, AutoRegister]
    public class ConditionDataManager : IConditionDataManager
    {
        Dictionary<int, ConditionJsonData> _conditionData = new Dictionary<int, ConditionJsonData>();
        Dictionary<string, ConditionJsonData> _conditionDataByName = new Dictionary<string, ConditionJsonData>();

        Dictionary<int, ConditionSourcesJsonData> _conditionSourceData = new Dictionary<int, ConditionSourcesJsonData>();
        Dictionary<string, ConditionSourcesJsonData> _conditionSourceDataByName = new Dictionary<string, ConditionSourcesJsonData>();

        public ConditionDataManager(ConditionDataProvider provider)
        {
            LoadConditions(provider.GetConditions());
            LoadConditionSources(provider.GetConditionSources());
        }

        private void LoadConditions(IEnumerable<ConditionJsonData> data)
        {
            foreach (var condition in data)
            {
                _conditionData.Add(condition.Id, condition);
                _conditionDataByName.Add(condition.Name, condition);
            }
        }

        private void LoadConditionSources(IEnumerable<ConditionSourcesJsonData> data)
        {
            foreach (var source in data)
            {
                _conditionSourceData.Add(source.Id, source);
                _conditionSourceDataByName.Add(source.Name, source);
            }
        }

        public ConditionJsonData GetConditionData(int id)
        {
            if (!_conditionData.ContainsKey(id))
                throw new NullReferenceException();

            return _conditionData[id];
        }

        public ConditionJsonData GetConditionData(string name)
        {
            if (!_conditionDataByName.ContainsKey(name))
                throw new NullReferenceException();

            return _conditionDataByName[name];
        }

        public ConditionSourcesJsonData GetConditionSourceData(int id)
        {
            if (!_conditionSourceData.ContainsKey(id))
                throw new NullReferenceException();

            return _conditionSourceData[id];
        }

        public ConditionSourcesJsonData GetConditionSourceData(string name)
        {
            if (!_conditionSourceDataByName.ContainsKey(name))
                throw new NullReferenceException();

            return _conditionSourceDataByName[name];
        }
    }
}
