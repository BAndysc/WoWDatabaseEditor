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

    [UniqueProvider]
    public interface IConditionDataManager
    {
        ConditionJsonData GetConditionData(int id);
        ConditionJsonData GetConditionData(string name);

        bool HasConditionData(int id);
        bool HasConditionData(string typeName);

        ConditionSourcesJsonData GetConditionSourceData(int id);
        ConditionSourcesJsonData GetConditionSourceData(string name);
    }

    [SingleInstance, AutoRegister]
    public class ConditionDataManager : IConditionDataManager
    {
        Dictionary<int, ConditionJsonData> conditionData = new ();
        Dictionary<string, ConditionJsonData> conditionDataByName = new ();

        Dictionary<int, ConditionSourcesJsonData> conditionSourceData = new ();
        Dictionary<string, ConditionSourcesJsonData> conditionSourceDataByName = new ();

        public ConditionDataManager(IConditionDataProvider provider)
        {
            LoadConditions(provider.GetConditions());
            LoadConditionSources(provider.GetConditionSources());
        }

        private void LoadConditions(IEnumerable<ConditionJsonData> data)
        {
            foreach (var condition in data)
            {
                conditionData.Add(condition.Id, condition);
                conditionDataByName.Add(condition.Name, condition);
            }
        }

        private void LoadConditionSources(IEnumerable<ConditionSourcesJsonData> data)
        {
            foreach (var source in data)
            {
                conditionSourceData.Add(source.Id, source);
                conditionSourceDataByName.Add(source.Name, source);
            }
        }

        public bool HasConditionData(int id)
        {
            return conditionData.ContainsKey(id);
        }

        public bool HasConditionData(string typeName)
        {
            return conditionDataByName.ContainsKey(typeName);
        }

        public ConditionJsonData GetConditionData(int id)
        {
            if (!conditionData.ContainsKey(id))
                throw new NullReferenceException();

            return conditionData[id];
        }

        public ConditionJsonData GetConditionData(string name)
        {
            if (!conditionDataByName.ContainsKey(name))
                throw new NullReferenceException();

            return conditionDataByName[name];
        }

        public ConditionSourcesJsonData GetConditionSourceData(int id)
        {
            if (!conditionSourceData.ContainsKey(id))
                throw new NullReferenceException();

            return conditionSourceData[id];
        }

        public ConditionSourcesJsonData GetConditionSourceData(string name)
        {
            if (!conditionSourceDataByName.ContainsKey(name))
                throw new NullReferenceException();

            return conditionSourceDataByName[name];
        }
    }
}