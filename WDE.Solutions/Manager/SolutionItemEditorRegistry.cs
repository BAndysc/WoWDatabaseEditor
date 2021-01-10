using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.Common.Managers;
using WDE.Common.Solution;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemEditorRegistry : ISolutionItemEditorRegistry
    {
        private Dictionary<Type, object> editorProviders = new Dictionary<Type, object>();

        public SolutionItemEditorRegistry(IEnumerable<ISolutionItemEditorProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (var provider in providers)
                Register((dynamic)provider);
        }

        private void Register<T>(ISolutionItemEditorProvider<T> provider) where T : ISolutionItem
        {
            editorProviders.Add(typeof(T), provider);
        }

        private IDocument GetEditor<T>(T item) where T : ISolutionItem
        {
            var x = editorProviders[item.GetType()] as ISolutionItemEditorProvider<T>;
            return x.GetEditor(item);
        }

        public IDocument GetEditor(ISolutionItem item)
        {
            return GetEditor((dynamic)item);
        }
    }
}
