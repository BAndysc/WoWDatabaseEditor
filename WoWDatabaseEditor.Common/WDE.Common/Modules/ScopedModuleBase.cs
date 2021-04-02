using System;
using System.Linq;
using Prism.Ioc;
using WDE.Module.Attributes;

namespace WDE.Module
{
    public abstract class ScopedModuleBase : ModuleBase
    {
        protected IScopedContainer moduleScope { get; }
        
        public ScopedModuleBase(IScopedContainer mainScope)
        {
            moduleScope = mainScope.CreateScope();
        }
        
        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterSelf(containerRegistry);
            AutoRegisterByConvention(moduleScope);
        }
        
        public override void FinalizeRegistration(IContainerRegistry container)
        {
            var typesToRegisterInParent = GetType().Assembly.GetTypes().Where(t => t.IsDefined(typeof(AutoRegisterToParentScopeAttribute), false) && !t.IsAbstract);

            foreach (var register in typesToRegisterInParent)
            {
                var instance = moduleScope.Resolve(register);
                foreach (Type @interface in register.GetInterfaces().Union(new[] {register}))
                {
                    string name = register + @interface.ToString();
                    container.RegisterInstance(@interface, instance, name);
                }
            }
        }
    }
}