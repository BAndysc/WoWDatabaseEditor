using Newtonsoft.Json.Linq;
using WDE.Module.Attributes;

namespace WDE.Common.Debugging;

[UniqueProvider]
public interface IDebuggerLogService
{
    void AddLog(DebugPointId debugId, string title, JObject log);
}