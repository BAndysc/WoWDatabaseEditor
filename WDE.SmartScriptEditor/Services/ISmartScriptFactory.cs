using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Services;

//[UniqueProvider]
public interface ISmartScriptFactory
{
    ISmartScriptSolutionItem Factory(int entryOrGuid, SmartScriptType type);
}
