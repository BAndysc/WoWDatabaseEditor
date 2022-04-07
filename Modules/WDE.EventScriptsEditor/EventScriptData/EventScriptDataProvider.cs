using System.Collections.Generic;
using System.Linq;
using LinqToDB.Reflection;
using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.EventScriptsEditor.EventScriptData;

[AutoRegister]
[SingleInstance]
public class EventScriptDataProvider : IEventScriptDataProvider
{
    private List<EventScriptRawData> data;
    private Dictionary<uint, EventScriptRawData> dataById;

    public EventScriptDataProvider(IEventScriptDataJsonProvider jsonProvider)
    {
        data = JsonConvert.DeserializeObject<List<EventScriptRawData>>(jsonProvider.GetJson());
        dataById = data.ToDictionary(x => x.Id, x => x);
    }

    public IReadOnlyList<EventScriptRawData> GetEventScriptData()
    {
        return data;
    }

    public EventScriptRawData? GetEventScriptData(uint id)
    {
        return dataById.TryGetValue(id, out var d) ? d : null;
    }
}