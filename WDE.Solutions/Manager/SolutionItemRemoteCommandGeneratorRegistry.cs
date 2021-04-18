using System;
using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemRemoteCommandGeneratorRegistry : ISolutionItemRemoteCommandGeneratorRegistry
    {
        private readonly Dictionary<Type, object> remoteCommandProviders = new();

        public SolutionItemRemoteCommandGeneratorRegistry(IEnumerable<ISolutionItemRemoteCommandProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemRemoteCommandProvider provider in providers)
                Register((dynamic) provider);
        }

        private void Register<T>(ISolutionItemRemoteCommandProvider<T> provider) where T : ISolutionItem
        {
            remoteCommandProviders.Add(typeof(T), provider);
        }

        private IRemoteCommand[] GenerateCommand<T>(T item) where T : ISolutionItem
        {
            if (remoteCommandProviders.TryGetValue(item.GetType(), out var provider))
            {
                return ((ISolutionItemRemoteCommandProvider<T>) provider).GenerateCommand(item);
            }

            return null;
        }

        public IRemoteCommand[] GenerateCommand(ISolutionItem item)
        {
            return GenerateCommand((dynamic) item);
        }
    }
}