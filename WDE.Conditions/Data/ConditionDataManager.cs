using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Modules;
using WDE.Common.Parameters;
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
        IEnumerable<ConditionGroupsJsonData> GetConditionGroups();
        IEnumerable<ConditionJsonData> AllConditionData { get; }
        IEnumerable<ConditionSourcesJsonData> AllConditionSourceData { get; }

        bool HasConditionData(int id);
        bool HasConditionData(string typeName);
        bool HasConditionSourceData(int id);
        bool HasConditionSourceData(string typeName);

        ConditionSourcesJsonData GetConditionSourceData(int id);
        ConditionSourcesJsonData GetConditionSourceData(string name);
    }

    [SingleInstance, AutoRegister]
    public class ConditionDataManager : IConditionDataManager, IGlobalAsyncInitializer
    {
        Dictionary<int, ConditionJsonData> conditionData = new ();
        Dictionary<string, ConditionJsonData> conditionDataByName = new ();

        Dictionary<int, ConditionSourcesJsonData> conditionSourceData = new ();
        Dictionary<string, ConditionSourcesJsonData> conditionSourceDataByName = new ();

        private IReadOnlyList<ConditionGroupsJsonData> conditionGroups = Array.Empty<ConditionGroupsJsonData>();

        private readonly IConditionDataProvider provider;
        private readonly IParameterFactory parameterFactory;

        public ConditionDataManager(IConditionDataProvider provider, IParameterFactory parameterFactory)
        {
            this.provider = provider;
            this.parameterFactory = parameterFactory;
        }

        public async Task Initialize()
        {
            LoadConditions(await provider.GetConditions());
            LoadConditionSources(await provider.GetConditionSources());
            conditionGroups = await provider.GetConditionGroups();

            RegisterDynamicParameters();
        }

        private void RegisterDynamicParameters()
        {
            foreach (var cond in conditionData.Values)
            {
                if (cond.Parameters == null)
                    continue;

                for (var index = 0; index < cond.Parameters.Count; index++)
                {
                    var param = cond.Parameters[index];
                    if (param.Values == null || param.Values.Count == 0)
                        continue;

                    string key = $"condition_{cond.Name}_{index}";
                    if (!parameterFactory.IsRegisteredLong(key))
                        parameterFactory.Register(key,
                            param.Type == "FlagParameter"
                                ? new FlagParameter() {Items = param.Values}
                                : new Parameter() {Items = param.Values});

                    param.Type = key;
                    cond.Parameters[index] = param;
                }
            }
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

        public bool HasConditionSourceData(int id)
        {
            return conditionSourceData.ContainsKey(id);
        }
        
        public bool HasConditionSourceData(string typeName)
        {
            return conditionSourceDataByName.ContainsKey(typeName);
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

        public IEnumerable<ConditionGroupsJsonData> GetConditionGroups() => conditionGroups;

        public IEnumerable<ConditionJsonData> AllConditionData => conditionDataByName.Values;

        public IEnumerable<ConditionSourcesJsonData> AllConditionSourceData => conditionSourceData.Values;
    }
}