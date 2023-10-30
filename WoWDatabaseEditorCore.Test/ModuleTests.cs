using NUnit.Framework;
using Prism.Ioc;
using Unity;
using WDE.Module;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Test;

public class ModuleTests
{
    [Test]
    public void TestModule()
    {
        var module = new Module();
        IUnityContainer unity = new UnityContainer();
        var ext = new Prism.Unity.Ioc.UnityContainerExtension(unity);
        module.InitializeCore("core");
        module.RegisterTypes(ext);
        var interfaceA = ext.Resolve<IInterfaceA>();
        var interfaceB = ext.Resolve<IInterfaceB>();
        Assert.AreEqual(0, interfaceA.A());
        Assert.AreEqual(1, interfaceA.A());
        Assert.AreEqual(2, interfaceB.B());
        Assert.AreEqual(3, interfaceA.A());
        Assert.AreSame(interfaceA, interfaceB);
    }
    
    internal interface IInterfaceA
    {
        int A();
    }
    
    internal interface IInterfaceB
    {
        int B();
    }

    [SingleInstance]
    internal class Implementation : IInterfaceA, IInterfaceB
    {
        private int x;
        public int A() => x++;
        public int B() => x++;
    }

    internal class Module : ModuleBase
    {
        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            RegisterType(typeof(Implementation), containerRegistry, false);
        }
    }
}