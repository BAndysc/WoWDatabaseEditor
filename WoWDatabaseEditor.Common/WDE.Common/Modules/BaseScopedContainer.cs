using System;
using Prism.Ioc;
using Unity;
using WDE.Module;

namespace WDE.Common.Modules
{
    public abstract class BaseScopedContainer : IScopedContainer
    {
        protected IUnityContainer unity;
        protected IContainerExtension inner;

        public BaseScopedContainer(IContainerExtension containerExtension, IUnityContainer impl)
        {
            inner = containerExtension;
            unity = impl;
        }
        
        public object Resolve(Type type) => inner.Resolve(type);

        public object Resolve(Type type, params (Type Type, object Instance)[] parameters) => inner.Resolve(type, parameters);

        public object Resolve(Type type, string name) => inner.Resolve(type, name);

        public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters) => inner.Resolve(type, name, parameters);

        public IContainerRegistry RegisterInstance(Type type, object instance) => inner.RegisterInstance(type, instance);

        public IContainerRegistry RegisterInstance(Type type, object instance, string name) => inner.RegisterInstance(type, instance, name);

        public IContainerRegistry RegisterSingleton(Type @from, Type to) => inner.RegisterSingleton(@from, to);

        public IContainerRegistry RegisterSingleton(Type @from, Type to, string name) => inner.RegisterSingleton(@from, to, name);

        public IContainerRegistry Register(Type @from, Type to) => inner.Register(@from, to);

        public IContainerRegistry Register(Type @from, Type to, string name) => inner.Register(@from, to, name);

        public bool IsRegistered(Type type) => inner.IsRegistered(type);

        public bool IsRegistered(Type type, string name) => inner.IsRegistered(type, name);

        public void FinalizeExtension() => inner.FinalizeExtension();

        public abstract IScopedContainer CreateScope();

        public IUnityContainer Instance => unity;

        public void Dispose()
        {
            unity.Dispose();
        }
    }
}