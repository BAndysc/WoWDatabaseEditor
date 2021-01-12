using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemEditorRegistry
    {
        IDocument GetEditor(ISolutionItem item);
    }
}