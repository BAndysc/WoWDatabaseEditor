using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Editor;

[AutoRegister]
public class TrinitySmartEditorExtension : ISmartEditorExtension
{
    public Task BeforeLoad(ISmartScriptSolutionItem item)
    {
        return Task.CompletedTask;
    }
}