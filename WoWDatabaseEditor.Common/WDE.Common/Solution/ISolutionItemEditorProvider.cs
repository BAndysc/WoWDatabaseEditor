using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemEditorProvider
    {
    }

    [NonUniqueProvider]
    public interface ISolutionItemEditorProvider<T> : ISolutionItemEditorProvider where T : ISolutionItem
    {
        IDocument GetEditor(T item);
    }
}