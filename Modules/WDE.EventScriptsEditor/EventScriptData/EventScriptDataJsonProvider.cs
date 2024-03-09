using System.IO;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.EventScriptsEditor.EventScriptData;

[AutoRegister]
[SingleInstance]
public class EventScriptDataJsonProvider : IEventScriptDataJsonProvider
{
    private readonly IRuntimeDataService runtimeDataService;

    public EventScriptDataJsonProvider(IRuntimeDataService runtimeDataService)
    {
        this.runtimeDataService = runtimeDataService;
    }

    public Task<string> GetJson() => runtimeDataService.ReadAllText("EventScriptData/EventScriptCommands.json");
}