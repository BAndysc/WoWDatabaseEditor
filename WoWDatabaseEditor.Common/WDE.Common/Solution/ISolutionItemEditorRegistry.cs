using System;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemEditorRegistry
    {
        IDocument GetEditor(ISolutionItem item);
    }

    public class SolutionItemEditorNotFoundException : Exception
    {
        public SolutionItemEditorNotFoundException(ISolutionItem item) : base($"Editor for type {item.GetType()} wasn't found") {}
    }
}