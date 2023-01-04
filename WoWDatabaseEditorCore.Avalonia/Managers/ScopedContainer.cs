using Prism.Ioc;
using Prism.Unity.Ioc;
using Unity;
using WDE.Common.Modules;
using WDE.Common.Utils;
using WDE.Module;

namespace WoWDatabaseEditorCore.Avalonia.Managers;

public class ScopedContainer : BaseScopedContainer
{
    public override IScopedContainer CreateScope()
    {
        var childContainer = unity.CreateChildContainer();
        var lt = new DefaultLifetime();
        childContainer.AddExtension(lt);
        lt.TypeDefaultLifetime = new ContainerControlledLifetimeManager();
        var extensions = new UnityContainerExtension(childContainer);
        var scope = new ScopedContainer(extensions, new UnityContainerRegistry(childContainer, extensions), childContainer);
        extensions.RegisterInstance<IScopedContainer>(scope);
        extensions.RegisterInstance<IContainerExtension>(scope);
        extensions.RegisterInstance<IContainerProvider>(scope);
        extensions.RegisterInstance<IContainerRegistry>(scope);
        return scope;
    }

    public ScopedContainer(IContainerProvider provider, IContainerRegistry registry, IUnityContainer impl) : base(provider, registry, impl)
    {
    }
}