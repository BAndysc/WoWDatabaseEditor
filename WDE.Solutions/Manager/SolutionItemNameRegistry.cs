﻿using System;
using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemNameRegistry : ISolutionItemNameRegistry
    {
        private readonly Dictionary<Type, object> nameProviders = new();

        public SolutionItemNameRegistry(IEnumerable<ISolutionNameProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionNameProvider provider in providers)
                Register((dynamic) provider);
        }

        public string GetName(ISolutionItem item)
        {
            return GetName((dynamic) item);
        }

        private void Register<T>(ISolutionNameProvider<T> provider) where T : ISolutionItem
        {
            nameProviders.Add(typeof(T), provider);
        }

        private string GetName<T>(T item) where T : ISolutionItem
        {
            if (nameProviders.TryGetValue(item.GetType(), out var nameProvider))
            {
                var x = (ISolutionNameProvider<T>)nameProvider;
                return x.GetName(item);
            }
#if DEBUG
            return item.GetType().Name + " (missing ISolutionNameProvider<T>)";
#else
            return item.GetType().Name;
#endif
        }
    }
}