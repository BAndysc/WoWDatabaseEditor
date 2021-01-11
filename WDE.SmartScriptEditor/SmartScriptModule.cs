using SmartFormat;
using WDE.Module;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptModule : ModuleBase
    {
        public SmartScriptModule() { Smart.Default.Parser.UseAlternativeEscapeChar(); }
    }
}