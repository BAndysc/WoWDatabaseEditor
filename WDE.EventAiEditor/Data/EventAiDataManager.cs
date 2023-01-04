using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.EventAiEditor.Editor;

namespace WDE.EventAiEditor.Data
{
    public enum EventOrAction
    {
        Event = 0,
        Action = 1
    }

    public interface IEventAiDataManager
    {
        bool Contains(EventOrAction type, uint id);

        bool Contains(EventOrAction type, string id);

        EventActionGenericJsonData GetRawData(EventOrAction type, uint id);

        EventActionGenericJsonData GetDataByName(EventOrAction type, string name);

        IEnumerable<EventAiGroupsJsonData> GetGroupsData(EventOrAction type);

        IEnumerable<EventActionGenericJsonData> GetAllData(EventOrAction type);
        
        void Reload(EventOrAction eventOrAction);
    }

    [AutoRegister]
    [SingleInstance]
    public class EventAiDataManager : IEventAiDataManager
    {
        private readonly Dictionary<EventOrAction, Dictionary<uint, EventActionGenericJsonData>> eventAiIdData = new();
        private readonly Dictionary<EventOrAction, Dictionary<string, EventActionGenericJsonData>> eventNameData = new();
        private readonly IEventAiDataProvider provider;
        private readonly IEditorFeatures editorFeatures;
        private readonly IParameterFactory parameterFactory;

        public EventAiDataManager(IEventAiDataProvider provider, 
            IEditorFeatures editorFeatures,
            IParameterFactory parameterFactory)
        {
            Load(EventOrAction.Event, provider.GetEvents());
            Load(EventOrAction.Action, provider.GetActions());
            this.provider = provider;
            this.editorFeatures = editorFeatures;
            this.parameterFactory = parameterFactory;

            foreach (var (key, value) in eventAiIdData)
                RegisterDynamicParameters(key, value.Values);
        }
        
        private void RegisterDynamicParameters(EventOrAction type, ICollection<EventActionGenericJsonData> datas)
        {
            foreach (var data in datas)
            {
                if (data.Parameters == null)
                    continue;

                for (var index = 0; index < data.Parameters.Count; index++)
                {
                    var param = data.Parameters[index];
                    if (param.Values == null || param.Values.Count == 0)
                        continue;

                    string key = $"{editorFeatures.Name}_{type}_{data.Name}_{index}";
                    if (!parameterFactory.IsRegisteredLong(key))
                        parameterFactory.Register(key,
                            param.Type == "FlagParameter"
                                ? new FlagParameter() {Items = param.Values}
                                : new Parameter() {Items = param.Values});

                    param.Type = key;
                    data.Parameters[index] = param;
                }
            }
        }

        public bool Contains(EventOrAction type, uint id)
        {
            if (!eventAiIdData.ContainsKey(type))
                return false;

            return eventAiIdData[type].ContainsKey(id);
        }

        public bool Contains(EventOrAction type, string id)
        {
            if (!eventNameData.ContainsKey(type))
                return false;

            return eventNameData[type].ContainsKey(id);
        }

        public EventActionGenericJsonData GetRawData(EventOrAction type, uint id)
        {
            if (!eventAiIdData[type].ContainsKey(id))
                throw new Exception("There is no " + type + " with id " + id);

            return eventAiIdData[type][id];
        }

        public EventActionGenericJsonData GetDataByName(EventOrAction type, string name)
        {
            if (!eventNameData[type].ContainsKey(name))
                throw new Exception("There is no " + type + " with name " + name);

            return eventNameData[type][name];
        }

        public IEnumerable<EventAiGroupsJsonData> GetGroupsData(EventOrAction type)
        {
            switch(type)
            {
                case EventOrAction.Event:
                    return provider.GetEventsGroups();
                case EventOrAction.Action:
                    return provider.GetActionsGroups();
                default:
                    return new List<EventAiGroupsJsonData>();
            }
        }

        public IEnumerable<EventActionGenericJsonData> GetAllData(EventOrAction type)
        {
            return eventAiIdData[type].Values;
        }

        private void Load(EventOrAction type, IEnumerable<EventActionGenericJsonData> data)
        {
            foreach (EventActionGenericJsonData d in data)
                Add(type, d);
        }

        private void Add(EventOrAction type, EventActionGenericJsonData data)
        {
            if (!eventAiIdData.ContainsKey(type))
            {
                eventAiIdData[type] = new Dictionary<uint, EventActionGenericJsonData>();
                eventNameData[type] = new Dictionary<string, EventActionGenericJsonData>();
            }

            if (!eventAiIdData[type].ContainsKey(data.Id))
            {
                eventAiIdData[type].Add(data.Id, data);
                eventNameData[type].Add(data.Name, data);
            }
            else
                throw new EventAiDataWithSuchIdExists($"{type} with id {data.Id} ({data.Name}) exists");
        }

        public void Reload(EventOrAction eventOrAction)
        {
            eventAiIdData.Remove(eventOrAction);
            eventNameData.Remove(eventOrAction);
            switch (eventOrAction)
            {
                case EventOrAction.Event:
                    Load(eventOrAction, provider.GetEvents());
                    break;
                case EventOrAction.Action:
                    Load(eventOrAction, provider.GetActions());
                    break;
            }
        }
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    public class EventAiDataWithSuchIdExists : Exception
    {
        public EventAiDataWithSuchIdExists()
        {
        }

        public EventAiDataWithSuchIdExists(string message) : base(message)
        {
        }

        public EventAiDataWithSuchIdExists(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}