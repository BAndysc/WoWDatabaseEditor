﻿using System;
using System.Linq;
using System.Reflection;
using Prism.Ioc;
using Prism.Modularity;
using Unity;
using Unity.Lifetime;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Module
{
    public abstract class ModuleBase : IModule
    {
        public ModuleBase()
        {
        }
        
        public virtual void OnInitialized(IContainerProvider containerProvider)
        {
            RegisterViews(containerProvider.Resolve<IViewLocator>());
        }

        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterSelf(containerRegistry);
            AutoRegisterByConvention(containerRegistry);
        }
        
        protected void RegisterSelf(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<ModuleBase>(this, GetType().FullName);
        }
        
        public virtual void RegisterViews(IViewLocator viewLocator) { }

        protected void AutoRegisterByConvention(IContainerRegistry containerRegistry)
        {
            AutoRegisterByConvention(GetType().Assembly, containerRegistry);
        }
        
        protected void AutoRegisterByConvention(Assembly assembly, IContainerRegistry containerRegistry)
        {
            //IUnityContainer unityContainer = ((IContainerExtension<IUnityContainer>)containerRegistry).Instance;
            var defaultRegisters = assembly.GetTypes().Where(t => !t.IsAbstract && t.IsDefined(typeof(AutoRegisterAttribute), true));

            foreach (Type register in defaultRegisters)
            {
                RegisterType(register, containerRegistry);
            }
        }

        protected void RegisterType(Type register, IContainerRegistry unityContainer)
        {
            bool singleton = register.IsDefined(typeof(SingleInstanceAttribute), false);

            if (singleton)
                unityContainer.RegisterSingleton(register);
                    
            foreach (Type @interface in register.GetInterfaces())
            {
                bool isUnique = !@interface.IsDefined(typeof(NonUniqueProviderAttribute), false);

                string name = register + @interface.ToString();

                if (singleton && isUnique)
                {
                    unityContainer.RegisterSingleton(@interface, register);
                    //unityContainer.RegisterFactory(@interface, c => unityContainer.Resolve(register), new ContainerControlledLifetimeManager());
                }
                else
                    unityContainer.Register(@interface, register, isUnique ? null : name);
            }
        }

        public virtual void FinalizeRegistration(IContainerRegistry container)
        {
        }
    }
}