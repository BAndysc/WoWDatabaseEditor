using System;
using Prism.Ioc;
using Unity;
using WDE.Module;

namespace WDE.Common.Modules
{
    public abstract class BaseScopedContainer : IScopedContainer
    {
        private readonly IContainerProvider provider;
        private readonly IContainerRegistry registry;
        protected IUnityContainer unity;

        public BaseScopedContainer(IContainerProvider provider, IContainerRegistry registry, IUnityContainer impl)
        {
            this.provider = provider;
            this.registry = registry;
            unity = impl;
        }
        
        public object Resolve(Type type) => provider.Resolve(type);

        public object Resolve(Type type, params (Type Type, object Instance)[] parameters) => provider.Resolve(type, parameters);

        public object Resolve(Type type, string name) => provider.Resolve(type, name);

        public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters) => provider.Resolve(type, name, parameters);

        public IContainerRegistry RegisterInstance(Type type, object instance) => registry.RegisterInstance(type, instance);

        public IContainerRegistry RegisterInstance(Type type, object instance, string name) => registry.RegisterInstance(type, instance, name);

        public IContainerRegistry RegisterSingleton(Type @from, Type to) => registry.Register(@from, to);

        public IContainerRegistry RegisterSingleton(Type @from, Type to, string name) => registry.RegisterSingleton(@from, to, name);

        public IContainerRegistry Register(Type @from, Type to) => registry.Register(@from, to);

        public IContainerRegistry Register(Type @from, Type to, string name) => registry.Register(@from, to, name);

        public bool IsRegistered(Type type) => registry.IsRegistered(type);

        public bool IsRegistered(Type type, string name) => registry.IsRegistered(type, name);

        public void FinalizeExtension() {}

        public abstract IScopedContainer CreateScope();

        public IUnityContainer Instance => unity;

        public void Dispose()
        {
            unity.Dispose();
        }
    }
}