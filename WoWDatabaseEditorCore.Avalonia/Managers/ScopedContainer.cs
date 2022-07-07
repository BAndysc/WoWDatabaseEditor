using JetBrains.Annotations;
using Prism.Ioc;
using Prism.Unity.Ioc;
using Unity;
using WDE.Common.Modules;
using WDE.Module;

namespace WoWDatabaseEditorCore.Avalonia.Managers;

public class ScopedContainer : BaseScopedContainer
{
    public override IScopedContainer CreateScope()
    {
        var childContainer = unity.CreateChildContainer();
        var extensions = new UnityContainerExtension(childContainer);
        var scope = new ScopedContainer(extensions, childContainer);
        extensions.RegisterInstance<IScopedContainer>(scope);
        extensions.RegisterInstance<IContainerExtension>(scope);
        extensions.RegisterInstance<IContainerProvider>(scope);
        extensions.RegisterInstance<IContainerRegistry>(scope);
        return scope;
    }

    public ScopedContainer(IContainerExtension containerExtension, IUnityContainer impl) : base(containerExtension, impl)
    {
    }
}