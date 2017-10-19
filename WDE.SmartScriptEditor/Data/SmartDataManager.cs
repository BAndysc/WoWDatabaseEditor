using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Data
{
    public enum SmartType
    {
        SmartEvent = 0,
        SmartAction = 1,
        SmartTarget = 2,
        SmartCondition = 3,
        SmartConditionSource = 4,
        SmartSource = 5,
    }

    public class SmartDataManager
    {
        private readonly Dictionary<SmartType, Dictionary<int, SmartGenericJsonData>> _smartIdData = new Dictionary<SmartType, Dictionary<int, SmartGenericJsonData>>();
        private readonly Dictionary<SmartType, Dictionary<string, SmartGenericJsonData>> _smartNameData = new Dictionary<SmartType, Dictionary<string, SmartGenericJsonData>>();

        public bool Contains(SmartType type, int id)
        {
            if (!_smartIdData.ContainsKey(type))
                return false;

            return _smartIdData[type].ContainsKey(id);
        }

        public bool Contains(SmartType type, string id)
        {
            if (!_smartNameData.ContainsKey(type))
                return false;

            return _smartNameData[type].ContainsKey(id);
        }

        private void ActualAdd(SmartType type, SmartGenericJsonData data)
        {
            if (!_smartIdData.ContainsKey(type))
            {
                _smartIdData[type] = new Dictionary<int, SmartGenericJsonData>();
                _smartNameData[type] = new Dictionary<string, SmartGenericJsonData>();
            }

            if (!_smartIdData[type].ContainsKey(data.Id))
            {
                if (data.ValidTypes != null && data.ValidTypes.Contains(0))
                    data.ValidTypes.Add(SmartScriptType.Creature);
                _smartIdData[type].Add(data.Id, data);
                _smartNameData[type].Add(data.Name, data);
            }
            else
                throw new SmartDataWithSuchIdExists();
        }

        public void Add(SmartType type, SmartGenericJsonData data)
        {
            if (type == SmartType.SmartSource)
                ActualAdd(SmartType.SmartTarget, data);
            else if (type == SmartType.SmartTarget && !data.IsOnlyTarget)
                ActualAdd(SmartType.SmartSource, data);

            ActualAdd(type, data);
        }

        public SmartGenericJsonData GetRawData(SmartType type, int id)
        {
            if (!_smartIdData[type].ContainsKey(id))
                throw new NullReferenceException();

            return _smartIdData[type][id];
        }
        
        public SmartGenericJsonData GetDataByName(SmartType type, string name)
        {
            if (!_smartNameData[type].ContainsKey(name))
                throw new NullReferenceException();

            return _smartNameData[type][name];
        }
        
        private static SmartDataManager _instance;
        public static SmartDataManager GetInstance()
        {
            return _instance ?? (_instance = new SmartDataManager());
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
