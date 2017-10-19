using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WDE.SmartScriptEditor.Data
{
    public class SmartDataLoader
    {
        public static void Load(SmartDataManager manager, ISmartDataProvider provider)
        {
            Load(SmartType.SmartEvent, provider.GetEventsJson(), manager);
            Load(SmartType.SmartAction, provider.GetActionsJson(), manager);
            Load(SmartType.SmartTarget, provider.GetTargetsJson(), manager);
        }

        private static void Load(SmartType type, string data, SmartDataManager manager)
        {
            var smartGenerics = JsonConvert.DeserializeObject<List<SmartGenericJsonData>>(data);
            smartGenerics.ForEach(e => manager.Add(type, e));
        }
    }
}
