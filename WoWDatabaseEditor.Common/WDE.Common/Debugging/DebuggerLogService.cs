using Newtonsoft.Json.Linq;
using WDE.Module.Attributes;

namespace WDE.Common.Debugging;

[FallbackAutoRegister]
public class DebuggerLogService : IDebuggerLogService
{
    public void AddLog(DebugPointId debugId, string title, JObject log)
    {

    }
}