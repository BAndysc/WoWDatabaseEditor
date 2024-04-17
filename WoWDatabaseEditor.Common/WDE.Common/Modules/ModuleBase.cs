using System;
using System.Linq;
using System.Reflection;
using Prism.Ioc;
using Prism.Modularity;
using Unity;
using Unity.Lifetime;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Module
{
    public interface IEditorModule : IModule
    {
        void InitializeCore(string tag);
    }
    
    public abstract class ModuleBase : IEditorModule
    {
        protected string? coreTag;

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
            Platforms currentPlatform = 0;
            if (OperatingSystem.IsAndroid())
                currentPlatform = Platforms.Android;
            else if (OperatingSystem.IsIOS())
                currentPlatform = Platforms.iOS;
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
                currentPlatform = Platforms.Desktop;
            else if (OperatingSystem.IsBrowser())
                currentPlatform = Platforms.Browser;
            else
                throw new Exception("Unknown platform");
            
            //IUnityContainer unityContainer = ((IContainerExtension<IUnityContainer>)containerRegistry).Instance;
            var defaultRegisters = assembly.GetTypes().Where(t =>
            {
                if (t.IsAbstract)
                    return false;
                
                var autoRegister = t.GetCustomAttribute<AutoRegisterAttribute>(true);
                if (autoRegister == null)
                    return false;

                if (autoRegister.RequiredPlatforms == Platforms.None)
                    return true;
                
                return (autoRegister.RequiredPlatforms & currentPlatform) != 0;
            });

            foreach (Type register in defaultRegisters)
            {
                RegisterType(register, containerRegistry, false);
            }
        }

        protected void RegisterType(Type register, IContainerRegistry unityContainer, bool checkIfRegistered)
        {
            if (string.IsNullOrEmpty(coreTag))
                throw new Exception("Core must be set before registering the types!");
            var requiresCore = register.GetCustomAttribute<RequiresCoreAttribute>();
            var rejectsCore = register.GetCustomAttribute<RejectsCoreAttribute>();
            bool singleton = register.IsDefined(typeof(SingleInstanceAttribute), false);

            if (requiresCore != null && !requiresCore.Tags.Contains(coreTag))
                return;
            
            if (rejectsCore != null && rejectsCore.Tags.Contains(coreTag))
                return;
            
            if (singleton)
                unityContainer.RegisterSingleton(register);

            IUnityContainer uc = ((IContainerExtension<IUnityContainer>)unityContainer).Instance;
            
            foreach (Type @interface in register.GetInterfaces())
            {
                if (checkIfRegistered && unityContainer.IsRegistered(@interface, UnityContainer.All))
                    continue;
                else if (checkIfRegistered)
                    LOG.LogInformation("Registering " + @interface + " as " + register + " because it's not registered yet");

                bool isUnique = !@interface.IsDefined(typeof(NonUniqueProviderAttribute), false);

                string name = register + @interface.ToString();

                if (singleton && isUnique)
                {
                    //unityContainer.RegisterSingleton(@interface, register);
                    uc.RegisterFactory(@interface, c => c.Resolve(register), new ContainerControlledLifetimeManager());
                }
                else
                {
                    if (singleton)
                        uc.RegisterFactory(@interface, isUnique ? null : name, c => c.Resolve(register), new ContainerControlledLifetimeManager());
                    else
                        unityContainer.Register(@interface, register, isUnique ? null : name);
                }
            }
        }

        protected void AutoRegisterFallbackTypesByConvention(Assembly assembly, IContainerRegistry container)
        {
            var fallbackRegisters = assembly.GetTypes().Where(t => !t.IsAbstract && t.IsDefined(typeof(FallbackAutoRegisterAttribute), true));

            foreach (Type register in fallbackRegisters)
            {
                RegisterType(register, container, true);
            }
        }

        public virtual void RegisterFallbackTypes(IContainerRegistry container)
        {
            AutoRegisterFallbackTypesByConvention(GetType().Assembly, container);
        }
        
        public virtual void FinalizeRegistration(IContainerRegistry container)
        {
        }

        public void InitializeCore(string coreTag)
        {
            this.coreTag = coreTag;
        }
    }
}
