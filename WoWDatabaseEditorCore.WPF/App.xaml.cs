using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Unity;
using Unity.RegistrationByConvention;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WDE.Common.WPF;
using WDE.Common.WPF.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.ModulesManagement;
using WoWDatabaseEditorCore.ViewModels;
using WoWDatabaseEditorCore.WPF.Views;

namespace WoWDatabaseEditorCore.WPF
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
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

                assemblyToRequesting.Add(name.Name ?? "", requestingAssemblyPath);

                AssemblyDependencyResolver? dependencyPathResolver = new(requestingAssemblyPath);
                string? path = dependencyPathResolver.ResolveAssemblyToPath(name);

                if (path == null)
                    return null;

                if (AssemblyLoadContext.Default.Assemblies.FirstOrDefault(t => t.GetName() == name) != null)
                    return AssemblyLoadContext.Default.Assemblies.FirstOrDefault(t => t.GetName() == name);

                return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            };
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            return new UnityContainerExtension(new UnityContainer().AddExtension(new Diagnostic()));
        }

        protected override Window CreateShell()
        {
            splash = Container.Resolve<SplashScreenView>();

            splash.Show();

            return splash;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(Container);
            modulesManager = new ModulesManager();
            var mainThread = new MainThread();
            GlobalApplication.InitializeApplication(mainThread);
            containerRegistry.RegisterInstance<IMainThread>(mainThread);
            containerRegistry.RegisterInstance(modulesManager);
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
            moduleCatalog.AddModule(typeof(CommonWpfModule));
            moduleCatalog.AddModule(typeof(MainModuleWPF));

            List<Assembly> allAssemblies = GetPluginDlls().Select(AssemblyLoadContext.Default.LoadFromAssemblyPath).ToList();

            var conflicts = DetectConflicts(allAssemblies);

            foreach (var conflict in conflicts)
            {
                MessageBox.Show(
                    $"Module {conflict.ConflictingAssembly.GetName().Name} conflicts with module {conflict.FirstAssembly.GetName().Name}. They provide same functionality. This is not allowed. Disablig {conflict.ConflictingAssembly.GetName().Name}");
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
            return Directory.GetFiles(path, "WDE*.dll").Where(path => !path.Contains("Test.dll") && !path.Contains("WDE.Common.WPF"));
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
                modulesManager!.AddModule(module.Assembly);

            modules.Select(module => new ModuleInfo
                {
                    ModuleName = module.Name,
                    ModuleType = module.AssemblyQualifiedName,
                    Ref = "file://" + module.Assembly.Location
                })
                .ToList()
                .ForEach(info => moduleCatalog.AddModule(info));
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        protected override void OnInitialized()
        {
            IMessageBoxService messageBoxService = Container.Resolve<IMessageBoxService>();
            IClipboardService clipboardService = Container.Resolve<IClipboardService>();
            ViewBind.AppViewLocator = Container.Resolve<IViewLocator>();
            // have no idea if it makes sense, but works
            MainWindow? mainWindow = Container.Resolve<MainWindow>();
            mainWindow.DataContext = Container.Resolve<MainWindowViewModel>();

            IEventAggregator? eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<AllModulesLoaded>().Publish();

            mainWindow.ContentRendered += MainWindowOnContentRendered;

            #if DEBUG
                mainWindow.ShowDialog();
            #else
                try
                {
                    mainWindow.ShowDialog();
                }
                catch (Exception e)
                {
                    var commitHash = File.Exists("COMMIT_HASH") ? File.ReadAllText("COMMIT_HASH") : "unknown";
                    Console.WriteLine(e.ToString());
                    var logPath = Path.GetTempFileName() + ".WDE.log.txt";
                    File.WriteAllText(logPath, "Commit: " + commitHash + "\n\n" + e.ToString());

                    var choice = messageBoxService.ShowDialog(new MessageBoxFactory<int>().SetIcon(MessageBoxIcon.Error)
                        .SetTitle("Fatal error")
                        .SetMainInstruction("Sorry, fatal error has occured and the program had to stop")
                        .SetContent("You are welcome to report the bug at github. Reason: " + e.Message)
                        .SetFooter("Log file saved at: " + logPath)
                        .WithButton("Copy log path to clipboard", 2)
                        .WithButton("Open log file", 1)
                        .WithButton("Close", 0)
                        .Build());
                    if (choice == 2)
                        clipboardService.SetText(logPath);
                    else if (choice == 1)
                        Process.Start("explorer", logPath);
                }
            #endif

            Current.Shutdown();
        }

        private void MainWindowOnContentRendered(object? sender, EventArgs e)
        {
            splash!.Close();
            (sender! as MainWindow)!.ContentRendered -= MainWindowOnContentRendered;
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
            Application.Current.Dispatcher.Invoke(action);
        }
    }
}