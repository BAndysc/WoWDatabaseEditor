using System;
using System.Linq;
using Prism.Ioc;
using Prism.Modularity;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Module
{
    public abstract class ModuleBase : IModule
    {
        public virtual void OnInitialized(IContainerProvider containerProvider)
        {
            RegisterViews(containerProvider.Resolve<IViewLocator>());
        }

        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            AutoRegisterByConvention(containerRegistry);
        }
        
        public virtual void RegisterViews(IViewLocator viewLocator) { }
        
        private void AutoRegisterByConvention(IContainerRegistry containerRegistry)
        {
            var defaultRegisters = GetType().Assembly.GetTypes().Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true));

            //HashSet<Type> alreadyInitialized = new HashSet<Type>();
            foreach (Type register in defaultRegisters)
            {
                if (register.IsAbstract)
                    continue;

                bool singleton = register.IsDefined(typeof(SingleInstanceAttribute), false);

                foreach (Type @interface in register.GetInterfaces().Union(new[] {register}))
                {
                    bool isUnique = !@interface.IsDefined(typeof(NonUniqueProviderAttribute), false);

                    string name = null;

                    //if (alreadyInitialized.Contains(interface_))
                    name = register + @interface.ToString();
                    //else
                    //     alreadyInitialized.Add(interface_);

                    //LifetimeManager life = null;

                    if (singleton && isUnique)
                        containerRegistry.RegisterSingleton(@interface, register);
                    //life = new ContainerControlledLifetimeManager();
                    else
                        containerRegistry.Register(@interface, register, isUnique ? null : name);
                    // life = new TransientLifetimeManager();

                    //Container.GetContainer().RegisterType(interface_, register, name, life);
                }
            }
        }
    }
}