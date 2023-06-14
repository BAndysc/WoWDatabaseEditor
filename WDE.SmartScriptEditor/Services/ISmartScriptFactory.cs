using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Services;

//[UniqueProvider]
public interface ISmartScriptFactory
{
    ISmartScriptSolutionItem Factory(uint? entry, int entryOrGuid, SmartScriptType type);
}
