using System.Threading.Tasks;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor;

public interface ISmartEditorExtension
{
    Task BeforeLoad(ISmartScriptSolutionItem item);
}