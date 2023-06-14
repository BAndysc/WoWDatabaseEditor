using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Services;

namespace WDE.TrinitySmartScriptEditor.Services;

[AutoRegister]
[SingleInstance]
public class SmartScriptFactory : ISmartScriptFactory
{
    public ISmartScriptSolutionItem Factory(uint? entry, int entryOrGuid, SmartScriptType type)
    {
        return new SmartScriptSolutionItem(entryOrGuid, type);
    }
}