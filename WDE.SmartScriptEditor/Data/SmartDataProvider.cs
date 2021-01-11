using System.Collections.Generic;
using Newtonsoft.Json;
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

        public IEnumerable<SmartGenericJsonData> GetActions() { return actions; }

        public IEnumerable<SmartGenericJsonData> GetEvents() { return events; }

        public IEnumerable<SmartGenericJsonData> GetTargets() { return targets; }
    }
}