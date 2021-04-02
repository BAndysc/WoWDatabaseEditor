using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    public enum SmartType
    {
        SmartEvent = 0,
        SmartAction = 1,
        SmartTarget = 2,
        SmartCondition = 3,
        SmartConditionSource = 4,
        SmartSource = 5
    }

    public interface ISmartDataManager
    {
        bool Contains(SmartType type, int id);

        bool Contains(SmartType type, string id);

        SmartGenericJsonData GetRawData(SmartType type, int id);

        SmartGenericJsonData GetDataByName(SmartType type, string name);

        IEnumerable<SmartGroupsJsonData> GetGroupsData(SmartType type);

        void Reload(SmartType smartType);
    }

    [AutoRegister]
    [SingleInstance]
    public class SmartDataManager : ISmartDataManager
    {
        private readonly Dictionary<SmartType, Dictionary<int, SmartGenericJsonData>> smartIdData = new();
        private readonly Dictionary<SmartType, Dictionary<string, SmartGenericJsonData>> smartNameData = new();
        private readonly ISmartDataProvider provider;
        
        public SmartDataManager(ISmartDataProvider provider)
        {
            Load(SmartType.SmartEvent, provider.GetEvents());
            Load(SmartType.SmartAction, provider.GetActions());
            Load(SmartType.SmartTarget, provider.GetTargets());
            this.provider = provider;
        }

        public bool Contains(SmartType type, int id)
        {
            if (!smartIdData.ContainsKey(type))
                return false;

            return smartIdData[type].ContainsKey(id);
        }

        public bool Contains(SmartType type, string id)
        {
            if (!smartNameData.ContainsKey(type))
                return false;

            return smartNameData[type].ContainsKey(id);
        }

        public SmartGenericJsonData GetRawData(SmartType type, int id)
        {
            if (!smartIdData[type].ContainsKey(id))
                throw new NullReferenceException();

            return smartIdData[type][id];
        }

        public SmartGenericJsonData GetDataByName(SmartType type, string name)
        {
            if (!smartNameData[type].ContainsKey(name))
                throw new NullReferenceException();

            return smartNameData[type][name];
        }

        public IEnumerable<SmartGroupsJsonData> GetGroupsData(SmartType type)
        {
            switch(type)
            {
                case SmartType.SmartEvent:
                    return provider.GetEventsGroups();
                case SmartType.SmartAction:
                    return provider.GetActionsGroups();
                case SmartType.SmartTarget:
                case SmartType.SmartSource:
                    return provider.GetTargetsGroups();
                default:
                    return new List<SmartGroupsJsonData>();
            }
        }

        private void Load(SmartType type, IEnumerable<SmartGenericJsonData> data)
        {
            foreach (SmartGenericJsonData d in data)
                Add(type, d);
        }

        private void ActualAdd(SmartType type, SmartGenericJsonData data)
        {
            if (!smartIdData.ContainsKey(type))
            {
                smartIdData[type] = new Dictionary<int, SmartGenericJsonData>();
                smartNameData[type] = new Dictionary<string, SmartGenericJsonData>();
            }

            if (!smartIdData[type].ContainsKey(data.Id))
            {
                smartIdData[type].Add(data.Id, data);
                smartNameData[type].Add(data.Name, data);
            }
            else
                throw new SmartDataWithSuchIdExists($"{type} with id {data.Id} ({data.Name}) exists");
        }

        private void Add(SmartType type, SmartGenericJsonData data)
        {
            if (type == SmartType.SmartSource)
                ActualAdd(SmartType.SmartTarget, data);
            else if (type == SmartType.SmartTarget && !data.IsOnlyTarget)
                ActualAdd(SmartType.SmartSource, data);

            ActualAdd(type, data);
        }

        public void Reload(SmartType smartType)
        {
            smartIdData.Remove(smartType);
            smartNameData.Remove(smartType);
            switch (smartType)
            {
                case SmartType.SmartEvent:
                    Load(smartType, provider.GetEvents());
                    break;
                case SmartType.SmartAction:
                    Load(smartType, provider.GetActions());
                    break;
                case SmartType.SmartTarget:
                case SmartType.SmartSource:
                    smartIdData.Remove(SmartType.SmartSource);
                    smartNameData.Remove(SmartType.SmartSource);
                    Load(smartType, provider.GetTargets());
                    break;
            }
        }
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    public class SmartDataWithSuchIdExists : Exception
    {
        public SmartDataWithSuchIdExists()
        {
        }

        public SmartDataWithSuchIdExists(string message) : base(message)
        {
        }

        public SmartDataWithSuchIdExists(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SmartDataWithSuchIdExists(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}