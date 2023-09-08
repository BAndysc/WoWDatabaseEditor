using System;
using System.Linq;
using Prism.Ioc;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace WDE.Common.Utils;

/// <summary>
/// <para>
/// Unity returns the same instance each time the Resolve(...) method is called or when the
/// dependency mechanism injects the instance into other classes.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// Per Container lifetime allows a registration of an existing or resolved object as
/// a scoped singleton in the container it was created or registered. In other words
/// this instance is unique within the container it war registered with. Child or parent
/// containers could have their own instances registered for the same contract.
/// </para>
/// <para>
/// When the <see cref="ContainerControlledLifetimeManager"/> is disposed,
/// the instance is disposed with it.
/// </para>
/// </remarks>
public class ContainerControlledLifetimeManager : SynchronizedLifetimeManager,
                                                  IInstanceLifetimeManager,
                                                  IFactoryLifetimeManager,
                                                  ITypeLifetimeManager
{
    #region Fields

    /// <summary>
    /// An instance of the object this manager is associated with.
    /// </summary>
    /// <value>This field holds a strong reference to the associated object.</value>
    protected object Value = NoValue;

    #endregion


    #region Constructor

    public ContainerControlledLifetimeManager()
    {
        Set    = base.SetValue;
        Get    = base.GetValue;
        TryGet = base.TryGetValue;
    }

    #endregion


    #region Scope

    public object? Scope { get; internal set; }

    #endregion


    #region SynchronizedLifetimeManager

    /// <inheritdoc/>
    public override object GetValue(ILifetimeContainer? container = null)
    {
        return Get(container);
    }

    /// <inheritdoc/>
    public override void SetValue(object newValue, ILifetimeContainer? container = null)
    {
        Set(newValue, container);
        Set = (o, c) => throw new InvalidOperationException("ContainerControlledLifetimeManager can only be set once");
        Get    = SynchronizedGetValue;
        TryGet = SynchronizedGetValue;
    }

    /// <inheritdoc/>
    protected override object SynchronizedGetValue(ILifetimeContainer? container = null) => Value;

    /// <inheritdoc/>
    protected override void SynchronizedSetValue(object newValue, ILifetimeContainer? container = null) => Value = newValue;

    /// <inheritdoc/>
    public override void RemoveValue(ILifetimeContainer? container = null) => Dispose();

    #endregion


    #region IFactoryLifetimeManager

    /// <inheritdoc/>
    protected override LifetimeManager OnCreateLifetimeManager()
    {
        return new ContainerControlledLifetimeManager
        {
            Scope = Scope
        };
    }

    #endregion


    #region IDisposable

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (NoValue == Value) return;
            // in this app, I invoke dispose manually
            // if (Value is IDisposable disposable)
            // {
            //     disposable.Dispose();
            // }
            Value = NoValue;
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    #endregion


    #region Overrides

    /// <summary>
    /// This method provides human readable representation of the lifetime
    /// </summary>
    /// <returns>Name of the lifetime</returns>
    public override string ToString() => "Lifetime:PerContainer";

    #endregion
}

public class UnityContainerRegistry : IContainerExtension<IUnityContainer>
{
    private readonly IUnityContainer container;
    private readonly IContainerExtension<IUnityContainer> extension;

    public UnityContainerRegistry(IUnityContainer container, IContainerExtension<IUnityContainer> extension)
    {
        this.container = container;
        this.extension = extension;
    }
    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        container.RegisterInstance(type, null, instance, new ContainerControlledLifetimeManager());
        //container.RegisterInstance(type, instance);
        return this;
    }

    public IContainerRegistry RegisterInstance(Type type, object instance, string name)
    {
        container.RegisterInstance(type, name, instance, new ContainerControlledLifetimeManager());
        //container.RegisterInstance(type, name, instance);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to)
    {
        container.RegisterType(from, to, null, new ContainerControlledLifetimeManager(), Array.Empty<InjectionMember>());
        //container.RegisterSingleton(from, to);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
    {
        container.RegisterType(from, to, name, new ContainerControlledLifetimeManager(), Array.Empty<InjectionMember>());
        //container.RegisterSingleton(from, to, name);
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

    public void FinalizeExtension()
    {
        extension.FinalizeExtension();
    }

    public object Resolve(Type type)
    {
        return extension.Resolve(type);
    }

    public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
    {
        return extension.Resolve(type, parameters);
    }

    public object Resolve(Type type, string name)
    {
        return extension.Resolve(type, name);
    }

    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
    {
        return extension.Resolve(type, name, parameters);
    }

    public IUnityContainer Instance => extension.Instance;
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