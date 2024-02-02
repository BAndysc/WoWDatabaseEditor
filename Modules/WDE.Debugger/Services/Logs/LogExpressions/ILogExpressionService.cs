using Newtonsoft.Json.Linq;

namespace WDE.Debugger.Services.Logs.LogExpressions;

internal interface ILogExpressionService
{
    JToken Parse(JObject root, string expression);
}