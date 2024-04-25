using System;
using System.Linq;
using System.Reflection;
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

        protected void RegisterToParentScope(Assembly assembly, IContainerRegistry parentContainer)
        {
            var typesToRegisterInParent = assembly.GetTypes().Where(t => t.IsDefined(typeof(AutoRegisterToParentScopeAttribute), false) && !t.IsAbstract);

            foreach (var register in typesToRegisterInParent)
            {
                var instance = moduleScope.Resolve(register);
                foreach (Type @interface in register.GetInterfaces().Union(new[] {register}))
                {
                    var isNonUnique = @interface.IsDefined(typeof(NonUniqueProviderAttribute), true);
                    if (isNonUnique)
                    {
                        string name = register + @interface.ToString();
                        parentContainer.RegisterInstance(@interface, instance, name);
                    }
                    else
                    {
                        parentContainer.RegisterInstance(@interface, instance);
                    }
                }
            }
        }
        
        public override void FinalizeRegistration(IContainerRegistry container)
        {
            RegisterToParentScope(GetType().Assembly, container);
        }
    }
}