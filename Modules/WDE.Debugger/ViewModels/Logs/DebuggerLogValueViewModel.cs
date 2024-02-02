using Newtonsoft.Json.Linq;
using WDE.Common.Utils;

namespace WDE.Debugger.ViewModels.Logs;

internal class DebuggerLogValueViewModel : IChildType
{
    public bool CanBeExpanded => false;
    public JToken Token { get; }
    public string Key { get; }
    public string Value { get; }
    public string? ValueExplained { get; }
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }

    public DebuggerLogValueViewModel(JToken token, string key, string value, string? valueExplained)
    {
        Token = token;
        Key = key;
        Value = value;
        ValueExplained = valueExplained;
    }
}