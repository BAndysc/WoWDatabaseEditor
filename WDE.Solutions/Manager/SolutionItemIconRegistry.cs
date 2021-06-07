using System;
using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemIconRegistry : ISolutionItemIconRegistry
    {
        private readonly Dictionary<Type, object> nameProviders = new();

        public SolutionItemIconRegistry(IEnumerable<ISolutionItemIconProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemIconProvider provider in providers)
                Register((dynamic) provider);
        }

        public ImageUri GetIcon(ISolutionItem item)
        {
            return GetIcon((dynamic) item);
        }

        private void Register<T>(ISolutionItemIconProvider<T> provider) where T : ISolutionItem
        {
            nameProviders.Add(typeof(T), provider);
        }

        private ImageUri GetIcon<T>(T item) where T : ISolutionItem
        {
            if (nameProviders.ContainsKey(item.GetType()))
            {
                var x = (ISolutionItemIconProvider<T>)nameProviders[item.GetType()];
                return x.GetIcon(item);                
            }

            return item.IsContainer ? new ImageUri("Icons/folder.png") : new ImageUri("Icons/document.png");
        }
    }
}