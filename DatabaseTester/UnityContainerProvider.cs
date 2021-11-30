using Prism.Ioc;
using Unity;
using Unity.Resolution;

namespace DatabaseTester;

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