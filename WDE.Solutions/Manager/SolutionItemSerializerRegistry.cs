using System;
using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemSerializerRegistry : ISolutionItemSerializerRegistry
    {
        private readonly Dictionary<Type, object> serializers = new();

        public SolutionItemSerializerRegistry(IEnumerable<ISolutionItemSerializer> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemSerializer provider in providers)
                Register((dynamic) provider);
        }

        public ISmartScriptProjectItem? Serialize(ISolutionItem item, bool forMostRecentlyUsed)
        {
            return Serialize((dynamic) item, forMostRecentlyUsed);
        }

        private void Register<T>(ISolutionItemSerializer<T> provider) where T : ISolutionItem
        {
            serializers.Add(typeof(T), provider);
        }

        private ISmartScriptProjectItem? Serialize<T>(T item, bool forMostRecentlyUsed) where T : ISolutionItem
        {
            if (!serializers.TryGetValue(item.GetType(), out var serializer))
                throw new Exception("No serializer for type " + item.GetType());

            ISolutionItemSerializer<T> x = (ISolutionItemSerializer<T>)serializer;
            return x.Serialize(item, forMostRecentlyUsed);
        }
    }
}