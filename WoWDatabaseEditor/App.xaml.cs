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
using WDE.Module.Attributes;
using WoWDatabaseEditor.Events;
using WoWDatabaseEditor.ModulesManagement;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private SplashScreenView splash;

        private IModulesManager modulesManager;

        protected override Window CreateShell()
        {
            splash = Container.Resolve<SplashScreenView>();

            splash.Show();

            return splash;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<IContainerProvider>(Container);
            modulesManager = new ModulesManager();
            containerRegistry.RegisterInstance<IModulesManager>(modulesManager);
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
            moduleCatalog.AddModule(typeof(MainModule));

            List<Assembly> allAssemblies = GetDlls().Select(path => Assembly.LoadFile(path)).ToList();

            var conflicts = DetectConflicts(allAssemblies);

            foreach (var conflict in conflicts)
            {
                MessageBox.Show($"Module {conflict.ConflictingAssembly.GetName().Name} conflicts with module {conflict.FirstAssembly.GetName().Name}. They provide same functionality. This is not allowed. Disablig {conflict.ConflictingAssembly.GetName().Name}");
                modulesManager.AddConflicted(conflict.ConflictingAssembly, conflict.FirstAssembly);
                allAssemblies.Remove(conflict.ConflictingAssembly);
            }
            
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

        private void AddMoulesFromLoadedAssemblies(IModuleCatalog moduleCatalog, List<Assembly> allAssemblies)
        {
            var modules = AllClasses.FromAssemblies(allAssemblies).Where(t => t.GetInterfaces().Contains(typeof(IModule)));

            foreach (var module in modules)
                modulesManager.AddModule(module.Assembly);

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
