using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Reflection;
using Newtonsoft.Json;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.EventScriptsEditor.EventScriptData;

[AutoRegister]
[SingleInstance]
public class EventScriptDataProvider : IEventScriptDataProvider, IGlobalAsyncInitializer
{
    private readonly IEventScriptDataJsonProvider jsonProvider;
    private readonly IRuntimeDataService runtimeDataService;
    private List<EventScriptRawData> data = new List<EventScriptRawData>();
    private Dictionary<uint, EventScriptRawData> dataById = new Dictionary<uint, EventScriptRawData>();

    public EventScriptDataProvider(IEventScriptDataJsonProvider jsonProvider,
        IRuntimeDataService runtimeDataService)
    {
        this.jsonProvider = jsonProvider;
        this.runtimeDataService = runtimeDataService;
    }

    public IReadOnlyList<EventScriptRawData> GetEventScriptData()
    {
        return data;
    }

    public EventScriptRawData? GetEventScriptData(uint id)
    {
        return dataById.TryGetValue(id, out var d) ? d : null;
    }

    public async Task Initialize()
    {
        data = JsonConvert.DeserializeObject<List<EventScriptRawData>>(await jsonProvider.GetJson())!;
        dataById = data.ToDictionary(x => x.Id, x => x);
    }
}