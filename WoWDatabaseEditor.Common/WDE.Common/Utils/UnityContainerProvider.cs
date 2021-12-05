using System;
using System.Linq;
using Prism.Ioc;
using Unity;
using Unity.Resolution;

namespace WDE.Common.Utils;

public class UnityContainerRegistry : IContainerRegistry
{
    private readonly IUnityContainer container;

    public UnityContainerRegistry(IUnityContainer container)
    {
        this.container = container;
    }
    
    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        container.RegisterInstance(type, instance);
        return this;
    }

    public IContainerRegistry RegisterInstance(Type type, object instance, string name)
    {
        container.RegisterInstance(type, instance);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type @from, Type to)
    {
        container.RegisterSingleton(@from, to);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type @from, Type to, string name)
    {
        container.RegisterSingleton(@from, to, name);
        return this;
    }

    public IContainerRegistry Register(Type @from, Type to)
    {
        container.RegisterType(@from, to);
        return this;
    }

    public IContainerRegistry Register(Type @from, Type to, string name)
    {
        container.RegisterType(@from, to, name);
        return this;
    }

    public bool IsRegistered(Type type)
    {
        return container.IsRegistered(type);
    }

    public bool IsRegistered(Type type, string name)
    {
        return container.IsRegistered(type, name);
    }
}

public class UnityContainerProvider : IContainerProvider
{
    private readonly IUnityContainer unityContainer;

    public UnityContainerProvider(IUnityContainer unityContainer)
    {
        this.unityContainer = unityContainer;
    }
    
    public object Resolve(Type type)
    {
        return unityContainer.Resolve(type);
    }

    public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
    {
        var overrides = parameters.Select(p => new DependencyOverride(p.Type, p.Instance)).ToArray();
        return unityContainer.Resolve(type, overrides);
    }

    public object Resolve(Type type, string name)
    {
        return unityContainer.Resolve(type, name);
    }

    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
    {
        var overrides = parameters.Select(p => new DependencyOverride(p.Type, p.Instance)).ToArray();
        return unityContainer.Resolve(type, name, overrides);
    }
}

public static class UnityExtensions
{
    public static T Resolve<T>(this IUnityContainer container)
    {
        return (T)container.Resolve(typeof(T));
    }
}