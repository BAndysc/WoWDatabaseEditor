using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Modules;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Editor;

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

        bool TryGetRawData(SmartType type, int id, out SmartGenericJsonData data);
        
        SmartGenericJsonData GetRawData(SmartType type, int id);

        SmartGenericJsonData GetDataByName(SmartType type, string name);

        Task<IReadOnlyList<SmartGroupsJsonData>> GetGroupsData(SmartType type);

        ReactiveProperty<IReadOnlyList<SmartGenericJsonData>> GetAllData(SmartType type);
        
        int MaxId(SmartType type);

        SmartGenericJsonData? GetDefaultEvent(SmartScriptType type);
    }

    [AutoRegister]
    [SingleInstance]
    public class SmartDataManager : ISmartDataManager, IGlobalAsyncInitializer
    {
        private readonly Dictionary<SmartType, Dictionary<int, SmartGenericJsonData>> smartIdData = new();
        private readonly Dictionary<SmartType, Dictionary<string, SmartGenericJsonData>> smartNameData = new();
        private readonly Dictionary<SmartType, ReactiveProperty<IReadOnlyList<SmartGenericJsonData>>> allData = new();
        private readonly Dictionary<(SmartType, SmartScriptType), int> defaults = new();
        private readonly ISmartDataProviderAsync provider;
        private readonly IEditorFeatures editorFeatures;
        private readonly IParameterFactory parameterFactory;

        private int maxIdEvent, maxIdAction, maxIdTarget;

        public SmartDataManager(ISmartDataProviderAsync provider, 
            IEditorFeatures editorFeatures,
            IParameterFactory parameterFactory,
            IStatusBar statusBar)
        {
            this.provider = provider;
            this.editorFeatures = editorFeatures;
            this.parameterFactory = parameterFactory;
            
            allData[SmartType.SmartAction] = new ReactiveProperty<IReadOnlyList<SmartGenericJsonData>>(new List<SmartGenericJsonData>());
            allData[SmartType.SmartEvent] = new ReactiveProperty<IReadOnlyList<SmartGenericJsonData>>(new List<SmartGenericJsonData>());
            allData[SmartType.SmartTarget] = allData[SmartType.SmartSource] = new ReactiveProperty<IReadOnlyList<SmartGenericJsonData>>(new List<SmartGenericJsonData>());

            provider.DefinitionsChanged += () =>
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Smart script definitions reloaded"));
                Initialize().ListenErrors();
            };
        }

        public async Task Initialize()
        {
            var events = await provider.GetEvents();
            var actions = await provider.GetActions();
            var targets = await provider.GetTargets();
            smartIdData.Clear();
            smartNameData.Clear();
            defaults.Clear();
            Load(SmartType.SmartEvent, events);
            Load(SmartType.SmartAction, actions);
            Load(SmartType.SmartTarget, targets);
            foreach (var (key, value) in smartIdData)
                RegisterDynamicParameters(key, value.Values);
        }

        private void RegisterDynamicParameters(SmartType type, ICollection<SmartGenericJsonData> datas)
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

                    if (param.Type != "FlagParameter" && param.Type != "SwitchParameter" && parameterFactory.IsRegisteredLong(param.Type))
                        continue; // SmartTarget will register its parameters, so SmartSource doesn't have to do it again

                    string key = $"{editorFeatures.Name}_{type}_{data.Name}_{index}";
                    parameterFactory.Register(key,
                        param.Type == "FlagParameter"
                            ? new FlagParameter() {Items = param.Values}
                            : new Parameter() {Items = param.Values}, overrideExisting: true);

                    param.Type = key;
                    data.Parameters[index] = param;
                }
            }
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

        public bool TryGetRawData(SmartType type, int id, out SmartGenericJsonData data)
        {
            if (!smartIdData.TryGetValue(type, out var smartList))
            {
                data = null!;
                return false;
            }

            if (smartList.TryGetValue(id, out var data_))
            {
                data = data_;
                return true;
            }

            data = null!;
            return false;
        }

        public SmartGenericJsonData GetRawData(SmartType type, int id)
        {
            if (!smartIdData[type].ContainsKey(id))
                throw new Exception("There is no " + type + " with id " + id);

            return smartIdData[type][id];
        }

        public SmartGenericJsonData GetDataByName(SmartType type, string name)
        {
            if (!smartNameData[type].ContainsKey(name))
                throw new Exception("There is no " + type + " with name " + name);

            return smartNameData[type][name];
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetGroupsData(SmartType type)
        {
            switch(type)
            {
                case SmartType.SmartEvent:
                    return await provider.GetEventsGroups();
                case SmartType.SmartAction:
                    return await provider.GetActionsGroups();
                case SmartType.SmartTarget:
                case SmartType.SmartSource:
                    return await provider.GetTargetsGroups();
                default:
                    return new List<SmartGroupsJsonData>();
            }
        }

        public ReactiveProperty<IReadOnlyList<SmartGenericJsonData>> GetAllData(SmartType type)
        {
            return allData[type];
        }

        private void Load(SmartType type, IEnumerable<SmartGenericJsonData> data)
        {
            if (type == SmartType.SmartEvent)
                maxIdEvent = 0;
            else if (type == SmartType.SmartAction)
                maxIdAction = 0;
            else if (type == SmartType.SmartTarget)
                maxIdTarget = 0;

            List<SmartGenericJsonData> list = new List<SmartGenericJsonData>();
            foreach (SmartGenericJsonData d in data)
            {
                list.Add(d);
                Add(type, d);
                if (type == SmartType.SmartEvent)
                    maxIdEvent = Math.Max(maxIdEvent, d.Id);
                else if (type == SmartType.SmartAction)
                    maxIdAction = Math.Max(maxIdAction, d.Id);
                else if (type == SmartType.SmartTarget)
                    maxIdTarget = Math.Max(maxIdTarget, d.Id);
            }
            allData[type].Value = list;
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

            if (data.DefaultFor != null)
            {
                foreach (var scriptType in Enum.GetValues<SmartScriptType>())
                    if (data.DefaultFor.Value.HasFlagFast(scriptType.ToMask()))
                        defaults[(type, scriptType)] = data.Id;
            }
        }

        private void Add(SmartType type, SmartGenericJsonData data)
        {
            if (type == SmartType.SmartSource)
                ActualAdd(SmartType.SmartTarget, data);
            else if (type == SmartType.SmartTarget && !data.IsOnlyTarget)
                ActualAdd(SmartType.SmartSource, data);

            ActualAdd(type, data);
        }

        //public void Reload(SmartType smartType)
        // {
        //     smartIdData.Remove(smartType);
        //     smartNameData.Remove(smartType);
        //     switch (smartType)
        //     {
        //         case SmartType.SmartEvent:
        //             Load(smartType, provider.GetEvents());
        //             break;
        //         case SmartType.SmartAction:
        //             Load(smartType, provider.GetActions());
        //             break;
        //         case SmartType.SmartTarget:
        //         case SmartType.SmartSource:
        //             smartIdData.Remove(SmartType.SmartSource);
        //             smartNameData.Remove(SmartType.SmartSource);
        //             Load(smartType, provider.GetTargets());
        //             break;
        //     }
        // }

        public int MaxId(SmartType type)
        {
            switch (type)
            {
                case SmartType.SmartEvent:
                    return maxIdEvent;
                case SmartType.SmartAction:
                    return maxIdAction;
                case SmartType.SmartTarget:
                case SmartType.SmartSource:
                    return maxIdTarget;
            }

            return 0;
        }

        public SmartGenericJsonData? GetDefaultEvent(SmartScriptType type)
        {
            if (defaults.TryGetValue((SmartType.SmartEvent, type), out var id) &&
                smartIdData[SmartType.SmartEvent].TryGetValue(id, out var data))
                return data;
            return null;
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
    }
}
