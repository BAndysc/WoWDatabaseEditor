using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Debugging;

public interface IScriptBreakpointsFactory
{
    IScriptBreakpoints? Create(SmartScript script);
}