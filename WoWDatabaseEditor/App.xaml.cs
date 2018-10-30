using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Unity.Lifetime;
using Unity.RegistrationByConvention;
using WDE.Common.Attributes;
using WoWDatabaseEditor.Events;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private SplashScreenView splash;

        protected override Window CreateShell()
        {
            splash = Container.Resolve<SplashScreenView>();

            splash.Show();

            return splash;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterRequiredTypes(containerRegistry);
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();

            containerRegistry.RegisterSingleton<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            List<Assembly> allAssemblies = GetDlls().Select(path => Assembly.LoadFile(path)).ToList();

            var conflicts = DetectConflicts(allAssemblies);

            foreach (var conflict in conflicts)
            {
                MessageBox.Show($"Module {conflict.ConflictingAssembly.GetName().Name} conflicts with module {conflict.FirstAssembly.GetName().Name}. They provide same functionality. This is not allowed. Disablig {conflict.ConflictingAssembly.GetName().Name}");
                allAssemblies.Remove(conflict.ConflictingAssembly);
            }

            AutoRegisterClasses(allAssemblies);

            AddMoulesFromLoadedAssemblies(moduleCatalog, allAssemblies);
        }
        
        private IEnumerable<string> GetDlls()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Directory.GetFiles(path, "*.dll");
        }

        private class Conflict
        {
            public Assembly ConflictingAssembly;
            public Assembly FirstAssembly;
        }

        private IList<Conflict> DetectConflicts(List<Assembly> allAssemblies)
        {
            Dictionary<Assembly, IList<Type>> providedInterfaces = new Dictionary<Assembly, IList<Type>>();

            List<Conflict> conflictingAssemblies = new List<Conflict>();

            foreach (var assembly in allAssemblies)
            {
                var implementedInterfaces = AllClasses.FromAssemblies(assembly).Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true)).SelectMany(t => t.GetInterfaces()).Where(t => t.IsDefined(typeof(UniqueProviderAttribute))).ToList();

                if (implementedInterfaces.Count == 0)
                    continue;

                foreach (var otherAssembly in providedInterfaces)
                {
                    var intersection = otherAssembly.Value.Intersect(implementedInterfaces).ToList();

                    if (intersection.Count > 0)
                        conflictingAssemblies.Add(new Conflict { ConflictingAssembly = assembly, FirstAssembly = otherAssembly.Key });
                }

                providedInterfaces.Add(assembly, implementedInterfaces.ToList());
            }

            return conflictingAssemblies;
        }

        /* 
         * Ok, this method is reaaly ugly. Luckily it is also realy low level
         * so it doesn't affect other parts that much. I don't know how it should be done properly.
         */
        private void AutoRegisterClasses(List<Assembly> allAssemblies)
        {
            var defaultRegisters = AllClasses.FromAssemblies(GetType().Assembly).Union(AllClasses.FromAssemblies(allAssemblies)).Distinct().Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true));

            HashSet<Type> alreadyInitialized = new HashSet<Type>();
            foreach (var register in defaultRegisters)
            {
                if (register.IsAbstract)
                    continue;

                var singleton = register.IsDefined(typeof(SingleInstanceAttribute), false);

                foreach (var interface_ in register.GetInterfaces().Union(new[] { register }))
                {
                    string name = null;

                    if (alreadyInitialized.Contains(interface_))
                        name = register.ToString() + interface_.ToString();
                    else
                        alreadyInitialized.Add(interface_);

                    LifetimeManager life = null;

                    if (singleton)
                        life = new ContainerControlledLifetimeManager();
                    else
                        life = new TransientLifetimeManager();

                    Container.GetContainer().RegisterType(interface_, register, name, life);
                }
            }
        }

        private void AddMoulesFromLoadedAssemblies(IModuleCatalog moduleCatalog, List<Assembly> allAssemblies)
        {
            var modules = AllClasses.FromAssemblies(GetType().Assembly).Union(AllClasses.FromAssemblies(allAssemblies)).Where(t => t.GetInterfaces().Contains(typeof(IModule)));

            modules.Select(module => new ModuleInfo()
            {
                ModuleName = module.Name,
                ModuleType = module.AssemblyQualifiedName,
                Ref = "file://" + module.Assembly.Location
            }).ToList().ForEach(moduleCatalog.AddModule);
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        protected override void OnInitialized()
        {
            // have no idea if it makes sense, but works
            var mainWindow = Container.Resolve<MainWindow>();

            var eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<AllModulesLoaded>().Publish();

            splash.Close();

            mainWindow.ShowDialog();
            Current.Shutdown();
        }
    }
}
