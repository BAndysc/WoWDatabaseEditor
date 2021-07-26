using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Prism.Unity.Ioc;
using Unity;
using Unity.RegistrationByConvention;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WDE.Module;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Managers;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService;
using WoWDatabaseEditorCore.Avalonia.Views;
using WoWDatabaseEditorCore.CoreVersion;
using WoWDatabaseEditorCore.Services.FileSystemService;
using WoWDatabaseEditorCore.Services.UserSettingsService;
using WoWDatabaseEditorCore.ModulesManagement;
using WoWDatabaseEditorCore.Services.DebugConsole;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class App : PrismApplication
    {
        private IModulesManager? modulesManager;
        private SplashScreenView? splash;

        public App()
        {
            /*
             * .net core (and .net 5) changed the way assembly and type resolving work.
             * Preferred way to implement "plugins" is using custom AssemblyLoadContext per plugin
             * however, current Prism implementation is not AssemblyLoadContext friendly
             * Therefore this workaround make assembly loading work more or less like in .net framework
             * All assemblies are loaded to the default context and any type can be found via Type.GetType()
             *
             * The disadvantage is that assemblies cannot conflict with each other. If using AssemblyLoadContext
             * there would be no problem with for instance different versions of a package.
             */
            Dictionary<string, string> assemblyToRequesting = new();
            string? executingAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.EndsWith("resources") || args.RequestingAssembly == null)
                    return null;

                AssemblyName? name = new(args.Name);

                string? requestingAssemblyPath = executingAssemblyLocation + "/" + args.RequestingAssembly.GetName().Name + ".dll";

                if (!File.Exists(requestingAssemblyPath))
                {
                    if (!assemblyToRequesting.TryGetValue(args.RequestingAssembly.GetName().Name ?? "", out requestingAssemblyPath))
                        return null;
                }

                assemblyToRequesting[name.Name ?? ""] = requestingAssemblyPath;

                AssemblyDependencyResolver? dependencyPathResolver = new(requestingAssemblyPath);
                string? path = dependencyPathResolver.ResolveAssemblyToPath(name);

                if (path == null)
                    return null;

                if (AssemblyLoadContext.Default.Assemblies.FirstOrDefault(t => t.GetName() == name) != null)
                    return AssemblyLoadContext.Default.Assemblies.FirstOrDefault(t => t.GetName() == name);

                return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            };
        }
        
        protected override Window? CreateShell()
        {
            return null;//Container.Resolve<SplashScreenView>();
        }
        
        protected override IContainerExtension CreateContainerExtension()
        {
            var unity = new UnityContainer().AddExtension(new Diagnostic());
            var container = new UnityContainerExtension(unity);
            var mainScope = new ScopedContainer(container, unity);
            container.RegisterInstance<IScopedContainer>(mainScope);
            return container;
        }
        
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(Container);
            var vfs = new VirtualFileSystem();
            var fs = new FileSystem(vfs);
            var userSettings = new UserSettings(fs, new DummyStatusBar());
            var currentCoreSettings = new CurrentCoreSettings(userSettings);
            
            modulesManager = new ModulesManager(currentCoreSettings);
            var mainThread = new MainThread();
            GlobalApplication.InitializeApplication(mainThread, GlobalApplication.AppBackend.Avalonia);
            containerRegistry.RegisterInstance<IMainThread>(mainThread);
            containerRegistry.RegisterInstance(modulesManager);
        }

        private class DummyStatusBar : IStatusBar
        {
            public void PublishNotification(INotification notification) { }
        }

        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterRequiredTypes(containerRegistry);
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule(typeof(MainModule));
            moduleCatalog.AddModule(typeof(MainModuleAvalonia));
            moduleCatalog.AddModule(typeof(CommonAvaloniaModule));
            
            List<Assembly> allAssemblies = GetPluginDlls()
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToList();
            
            allAssemblies.Sort(Comparer<Assembly>.Create((a, b) =>
            {
                var aRequires = a.GetCustomAttributes(typeof(ModuleRequiresCoreAttribute), false);
                var bRequires = b.GetCustomAttributes(typeof(ModuleRequiresCoreAttribute), false);
                return bRequires.Length.CompareTo(aRequires.Length);
            }));
            
            List<Assembly> loadAssemblies = allAssemblies
                .Where(modulesManager!.ShouldLoad)
                .ToList();

            var conflicts = DetectConflicts(loadAssemblies);

            foreach (var conflict in conflicts)
            {
                //MessageBox.Show(
                //    $"Module {conflict.ConflictingAssembly.GetName().Name} conflicts with module {conflict.FirstAssembly.GetName().Name}. They provide same functionality. This is not allowed. Disablig {conflict.ConflictingAssembly.GetName().Name}");
                modulesManager!.AddConflicted(conflict.ConflictingAssembly, conflict.FirstAssembly);
                allAssemblies.Remove(conflict.ConflictingAssembly);
            }

            AddMoulesFromLoadedAssemblies(moduleCatalog, allAssemblies);
        }

        private IEnumerable<string> GetPluginDlls()
        {
            string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (path == null)
                return ArraySegment<string>.Empty;
            return Directory.GetFiles(path, "WDE*.dll").Where(path => !path.Contains("Test.dll") && !path.Contains("WPF") && !path.Contains("WDE.Common.Avalonia.dll"));
        }

        private IList<Conflict> DetectConflicts(List<Assembly> allAssemblies)
        {
            Dictionary<Assembly, IList<Type>> providedInterfaces = new();

            List<Conflict> conflictingAssemblies = new();

            foreach (var assembly in allAssemblies)
            {
                var implementedInterfaces = AllClasses.FromAssemblies(assembly)
                    .Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true))
                    .SelectMany(t => t.GetInterfaces())
                    .Where(t => t.IsDefined(typeof(UniqueProviderAttribute)))
                    .ToList();

                if (!implementedInterfaces.Any())
                    continue;

                foreach (var otherAssembly in providedInterfaces)
                {
                    var intersection = otherAssembly.Value.Intersect(implementedInterfaces).ToList();

                    if (intersection.Count > 0)
                        conflictingAssemblies.Add(new Conflict(assembly, otherAssembly.Key));
                }

                providedInterfaces.Add(assembly, implementedInterfaces.ToList());
            }

            return conflictingAssemblies;
        }

        private void AddMoulesFromLoadedAssemblies(IModuleCatalog moduleCatalog, List<Assembly> allAssemblies)
        {
            var modules = AllClasses.FromAssemblies(allAssemblies).Where(t => t.GetInterfaces().Contains(typeof(IModule))).ToList();

            foreach (var module in modules)
            {
                bool load = modulesManager!.AddModule(module.Assembly);
                if (load)
                    moduleCatalog.AddModule(new ModuleInfo
                    {
                        ModuleName = module.Name,
                        ModuleType = module.AssemblyQualifiedName,
                        Ref = "file://" + module.Assembly.Location
                    });
            }
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        protected override void OnInitialized()
        {
            this.InitializeModules();

            var loadedModules = Container.Resolve<IEnumerable<ModuleBase>>();
            foreach (var module in loadedModules)
                module.FinalizeRegistration((IContainerRegistry)Container);

            Container.Resolve<IDebugConsole>();
            
            var themeManager = Container.Resolve<IThemeManager>();
            ViewBind.AppViewLocator = Container.Resolve<IViewLocator>();

            IMessageBoxService messageBoxService = Container.Resolve<IMessageBoxService>();
            ViewBind.AppViewLocator = Container.Resolve<IViewLocator>();
        }

        public static Window? MainApp;

        public override void Initialize()
        {
            base.Initialize();
            AvaloniaXamlLoader.Load(this);
            
            
            if (AvaloniaThemeStyle.UseDock)
            {
                ((IContainerRegistry)Container).RegisterSingleton<MainWindowWithDocking>();
                MainApp = Container.Resolve<MainWindowWithDocking>();
            }
            else
            {
                ((IContainerRegistry)Container).RegisterSingleton<MainWindow>();
                MainApp = Container.Resolve<MainWindow>();
            }
            MainApp.DataContext = Container.Resolve<MainWindowViewModel>();
            this.InitializeShell(MainApp);

            
            IEventAggregator? eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<AllModulesLoaded>().Publish();
        }

        private class Conflict
        {
            public readonly Assembly ConflictingAssembly;
            public readonly Assembly FirstAssembly;

            public Conflict(Assembly conflictingAssembly, Assembly firstAssembly)
            {
                ConflictingAssembly = conflictingAssembly;
                FirstAssembly = firstAssembly;
            }
        }
    }
    public class MainThread : IMainThread
    {
        public void Dispatch(Action action)
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}
