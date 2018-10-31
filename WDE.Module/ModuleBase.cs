using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Module
{
    public abstract class ModuleBase : IModule
    {
        public virtual void OnInitialized(IContainerProvider containerProvider)
        {
            
        }

        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            AutoRegisterByConvention(containerRegistry);
        }

        private void AutoRegisterByConvention(IContainerRegistry containerRegistry)
        {
            var defaultRegisters = GetType().Assembly.GetTypes().Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true));

            //HashSet<Type> alreadyInitialized = new HashSet<Type>();
            foreach (var register in defaultRegisters)
            {
                if (register.IsAbstract)
                    continue;

                var singleton = register.IsDefined(typeof(SingleInstanceAttribute), false);

                foreach (var interface_ in register.GetInterfaces().Union(new[] { register }))
                {
                    var isUnique = !interface_.IsDefined(typeof(NonUniqueProviderAttribute), false);

                    string name = null;

                    //if (alreadyInitialized.Contains(interface_))
                        name = register.ToString() + interface_.ToString();
                    //else
                    //     alreadyInitialized.Add(interface_);

                    //LifetimeManager life = null;

                    if (singleton && isUnique)
                        containerRegistry.RegisterSingleton(interface_, register);
                    //life = new ContainerControlledLifetimeManager();
                    else
                        containerRegistry.Register(interface_, register, isUnique ? null : name);
                        // life = new TransientLifetimeManager();

                        //Container.GetContainer().RegisterType(interface_, register, name, life);
                }
            }
        }
    }
}
