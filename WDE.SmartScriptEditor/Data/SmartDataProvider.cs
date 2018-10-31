using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    public class SmartDataProvider : ISmartDataProvider
    {
        private readonly List<SmartGenericJsonData> actions;
        private readonly List<SmartGenericJsonData> events;
        private readonly List<SmartGenericJsonData> targets;

        public SmartDataProvider(ISmartDataJsonProvider jsonProvider)
        {
            actions = JsonConvert.DeserializeObject<List<SmartGenericJsonData>>(jsonProvider.GetActionsJson());
            events = JsonConvert.DeserializeObject<List<SmartGenericJsonData>>(jsonProvider.GetEventsJson());
            targets = JsonConvert.DeserializeObject<List<SmartGenericJsonData>>(jsonProvider.GetTargetsJson());
        }

        public IEnumerable<SmartGenericJsonData> GetActions()
        {
            return actions;
        }

        public IEnumerable<SmartGenericJsonData> GetEvents()
        {
            return events;
        }

        public IEnumerable<SmartGenericJsonData> GetTargets()
        {
            return targets;
        }
    }
}
