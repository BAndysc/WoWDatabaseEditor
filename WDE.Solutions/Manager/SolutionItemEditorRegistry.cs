using System;
using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemEditorRegistry : ISolutionItemEditorRegistry
    {
        private readonly Dictionary<Type, object> editorProviders = new();

        public SolutionItemEditorRegistry(IEnumerable<ISolutionItemEditorProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemEditorProvider provider in providers)
                Register((dynamic) provider);
        }

        public IDocument GetEditor(ISolutionItem item)
        {
            return GetEditor((dynamic) item);
        }

        private void Register<T>(ISolutionItemEditorProvider<T> provider) where T : ISolutionItem
        {
            editorProviders.Add(typeof(T), provider);
        }

        private IDocument GetEditor<T>(T item) where T : ISolutionItem
        {
            if (!editorProviders.TryGetValue(item.GetType(), out var editor))
                throw new SolutionItemEditorNotFoundException(item);
            return ((ISolutionItemEditorProvider<T>)editor).GetEditor(item);
        }
    }
}