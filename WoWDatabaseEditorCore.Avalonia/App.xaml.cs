using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Classic.Avalonia.Theme;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Prism.Unity.Ioc;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Unity;
using Unity.RegistrationByConvention;
using WDE.Common;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Events;
using WDE.Common.Factories;
using WDE.Common.Managers;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.Module;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Managers;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers;
using WoWDatabaseEditorCore.Avalonia.Views;
using WoWDatabaseEditorCore.CoreVersion;
using WoWDatabaseEditorCore.Managers;
using WoWDatabaseEditorCore.Services.FileSystemService;
using WoWDatabaseEditorCore.Services.UserSettingsService;
using WoWDatabaseEditorCore.ModulesManagement;
using WoWDatabaseEditorCore.Services.DebugConsole;
using WoWDatabaseEditorCore.Services.LoadingEvents;
using WoWDatabaseEditorCore.Services.LogService.Logging;
using WoWDatabaseEditorCore.Services.LogService.ReportErrorsToServer;
using WoWDatabaseEditorCore.ViewModels;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogDataStore = WoWDatabaseEditorCore.Services.LogService.Logging.LogDataStore;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class App : PrismApplication
    {
        private IModulesManager? modulesManager;
        private ICurrentCoreSettings currentCoreSettings = null!;
        private ILoggerFactory loggerFactory;
        private ILogDataStore logDataStore;
        private ReportErrorsSink reportErrorsSink;
        private RestoreFocusAfterEnableChange? restoreFocusAfterEnableChange;

        public App()
        {
            reportErrorsSink = new ReportErrorsSink();

            logDataStore = new LogDataStore();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.DataStoreLoggerSink(() => logDataStore)
                .WriteTo.Sink(reportErrorsSink)
                .CreateLogger();

            loggerFactory = new SerilogLoggerFactory(Log.Logger);
            LOG.Initialize(loggerFactory);

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
            string? executingAssemblyLocation = System.AppContext.BaseDirectory;// Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
            IUnityContainer unity = new UnityContainer();
#if DEBUG
            unity = unity.AddExtension(new Diagnostic());
#endif
            var container = new UnityContainerExtension(unity);
            var mainScope = new ScopedContainer(container, container, unity);
            container.RegisterInstance<IScopedContainer>(mainScope);
            ViewBind.ContainerProvider = container;
            DI.Container = unity;
            return container;
        }

        private IUserSettings CreateUserSettings()
        {
            IUserSettings userSettings;
            if (OperatingSystem.IsBrowser())
            {
                userSettings = new WebUserSettings();
            }
            else
            {
                var vfs = new VirtualFileSystem();
                var fs = new FileSystem(vfs);
                userSettings = new UserSettings(fs, new Lazy<IStatusBar>(new DummyStatusBar()));
            }
            return userSettings;
        }
        
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(Container);
            currentCoreSettings = new CurrentCoreSettings(CreateUserSettings());
            containerRegistry.RegisterInstance(typeof(ICurrentCoreSettings), currentCoreSettings);
            
            modulesManager = new ModulesManager(currentCoreSettings);
            var mainThread = new MainThread();
            GlobalApplication.InitializeApplication(mainThread, GlobalApplication.AppBackend.Avalonia, IsSingleView);
            containerRegistry.RegisterInstance<IMainThread>(mainThread);
            containerRegistry.RegisterInstance(modulesManager);
        }

        private class DummyStatusBar : IStatusBar
        {
            public void PublishNotification(INotification notification) { }
            public INotification? CurrentNotification { get; set; }
            public event PropertyChangedEventHandler? PropertyChanged;
        }

        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterRequiredTypes(containerRegistry);
            containerRegistry.RegisterInstance<ILogDataStore>(logDataStore);
            containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);
            containerRegistry.RegisterInstance(reportErrorsSink);
            containerRegistry.RegisterSingleton<IModuleInitializer, CustomModuleInitializer>();
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
            containerRegistry.RegisterSingleton<IViewLocator, ViewLocator>();
            containerRegistry.RegisterSingleton<ILoadingEventAggregator, LoadingEventAggregator>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            List<Assembly> externalAssemblies = GetPluginDlls()
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToList();

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                {
                    var name = a.GetName().Name!;
                    return (name.Contains("WDE") && !name.Contains("Test") && !name.Contains("WDE.Common.Avalonia")) || (name.Contains("LoaderAvalonia"));
                })
                .ToList();

            allAssemblies.Sort(Comparer<Assembly>.Create((a, b) =>
            {
                var aRequires = a.GetCustomAttributes(typeof(ModuleRequiresCoreAttribute), false);
                var bRequires = b.GetCustomAttributes(typeof(ModuleRequiresCoreAttribute), false);
                return bRequires.Length.CompareTo(aRequires.Length);
            }));

            List<Assembly> loadAssemblies = allAssemblies;

            do
            {
                var newLimit = loadAssemblies.Where(modulesManager!.ShouldLoad).ToList();
                if (newLimit.Count == loadAssemblies.Count)
                    break;
                loadAssemblies = newLimit;
            } while (true);

            var conflicts = DetectConflicts(loadAssemblies);

            foreach (var conflict in conflicts)
            {
                LOG.LogWarning($"Conflicting assemblies: {conflict.ConflictingAssembly.GetName().Name} (won't load) and {conflict.FirstAssembly.GetName().Name}");
                modulesManager!.AddConflicted(conflict.ConflictingAssembly, conflict.FirstAssembly);
                allAssemblies.Remove(conflict.ConflictingAssembly);
            }

            AddMoulesFromLoadedAssemblies(moduleCatalog, allAssemblies);
            moduleCatalog.AddModule(typeof(MainModule));
            moduleCatalog.AddModule(typeof(MainModuleAvalonia));
            moduleCatalog.AddModule(typeof(CommonAvaloniaModule));
        }

        private IEnumerable<string> GetPluginDlls()
        {
            // uncomment for plugins support
            // #if !DEBUG
            // if (OperatingSystem.IsWindows())
            // {
            //     return Directory.GetFiles(".", "WDE*.dll")
            //         .Where(path => !path.Contains("Test.dll") && !path.Contains("WPF") && !path.Contains("WDE.Common.Avalonia.dll"))
            //         .Select(Path.GetFullPath);
            // }
            // #endif

            return Array.Empty<string>();
        }

        private IList<Conflict> DetectConflicts(List<Assembly> allAssemblies)
        {
            Dictionary<Assembly, IList<Type>> providedInterfaces = new();

            List<Conflict> conflictingAssemblies = new();

            foreach (var assembly in allAssemblies)
            {
                var implementedInterfaces = AllClasses.FromAssemblies(assembly)
                    .Where(t => t.IsDefined(typeof(AutoRegisterAttribute), true))
                    .Where(t =>
                    {
                        var requiresCoreAttribute = t.GetCustomAttribute<RequiresCoreAttribute>();
                        return requiresCoreAttribute == null || requiresCoreAttribute.Tags.Contains(currentCoreSettings.CurrentCore);
                    })
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
#pragma warning disable IL3000
                        Ref = "file://" + module.Assembly.Location
#pragma warning restore IL3000
                    });
            }
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        private Window? splashScreenWindow;

        private IGlobalServiceRoot? globalServiceRoot;
        
        protected async Task OnInitializedAsync()
        {
            restoreFocusAfterEnableChange = new RestoreFocusAfterEnableChange();
            this.InitializeModules();

            var loadedModules = Container.Resolve<IEnumerable<ModuleBase>>().ToList();
            
            foreach (var module in loadedModules)
                module.RegisterFallbackTypes((IContainerRegistry)Container);

            var asyncInitializers = Container.Resolve<IEnumerable<IGlobalAsyncInitializer>>();

            foreach (var init in asyncInitializers)
            {
                try
                {
                    await init.Initialize();
                }
                catch (Exception e)
                {
                    Container.Resolve<IMessageBoxService>()
                        .ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Fatal error")
                            .SetMainInstruction("Failed to initialize module")
                            .SetContent("Fatal error occured during module " + init.GetType() + " initialization. If you managed to see this error, then the editor recovered enough to show you this, but the editor is BUGGED now. Please report this error at github.com.\n\n" + e.Message+"\n" + e.StackTrace)
                            .WithButton("I will report on github", true)
                            .Build()).ListenWarnings();
                    LOG.LogCritical(e, $"Type {init.GetType()} async initialize exception");
                }
            }

            foreach (var module in loadedModules)
                module.FinalizeRegistration((IContainerRegistry)Container);

            Container.Resolve<IDebugConsole>();
            
            ViewBind.AppViewLocator = Container.Resolve<IViewLocator>();

            IMessageBoxService messageBoxService = Container.Resolve<IMessageBoxService>();
            ViewBind.AppViewLocator = Container.Resolve<IViewLocator>();
        }
        
        private bool forceSingleView = false;

        public bool IsSingleView => forceSingleView || ApplicationLifetime is ISingleViewApplicationLifetime;

        private async Task OnFrameworkInitializationCompletedAsync()
        {
            if (Design.IsDesignMode)
                return;
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                 splashScreenWindow = new SplashScreenWindow();
                 splashScreenWindow.Show();
                 await Task.Delay(1);
            }
            
            base.Initialize();
            
            await OnInitializedAsync();

            var dataContext = Container.Resolve<MainWindowViewModel>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                if (forceSingleView)
                {
                    var mainApp = Container.Resolve<MainWebView>();

                    mainApp.DataContext = dataContext;
                    var window = Container.Resolve<Window1>();
                    InitializeShell(window);
                    
                    globalServiceRoot = Container.Resolve<IGlobalServiceRoot>();

                    IEventAggregator? eventAggregator = Container.Resolve<IEventAggregator>();
                    eventAggregator.GetEvent<AllModulesLoaded>().Publish();
                    Container.Resolve<ILoadingEventAggregator>().Publish<EditorLoaded>();

                    window.Content = mainApp;
                    window.Title = dataContext.Title;
                    window.Show();
                }
                else
                {
                    var mainApp = Container.Resolve<MainWindowWithDocking>();
                
                    mainApp.DataContext = dataContext;
                    this.InitializeShell(mainApp);
                
                    globalServiceRoot = Container.Resolve<IGlobalServiceRoot>();
                
                    IEventAggregator? eventAggregator = Container.Resolve<IEventAggregator>();
                    eventAggregator.GetEvent<AllModulesLoaded>().Publish();
                    Container.Resolve<ILoadingEventAggregator>().Publish<EditorLoaded>();
                    mainApp.ShowActivated = true;
                    mainApp.Show();
                }
                splashScreenWindow?.Close();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                var mainApp = Container.Resolve<MainWebView>();

                mainApp.DataContext = dataContext;
                this.InitializeShell(mainApp);

                globalServiceRoot = Container.Resolve<IGlobalServiceRoot>();

                IEventAggregator? eventAggregator = Container.Resolve<IEventAggregator>();
                eventAggregator.GetEvent<AllModulesLoaded>().Publish();
                Container.Resolve<ILoadingEventAggregator>().Publish<EditorLoaded>();

                singleView.MainView = mainApp;
            }

            Container.Resolve<ReportErrorsSinkManager>();
        }
        
        public override void OnFrameworkInitializationCompleted()
        {
            async Task Load()
            {
                try
                {
                    await OnFrameworkInitializationCompletedAsync();
                    base.OnFrameworkInitializationCompleted();
                }
                catch (Exception e)
                {
                    FatalErrorHandler.ExceptionOccured(e, GlobalApplication.Arguments.ToArray());
                    (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                }
            }
            Load().ListenErrors();

        }

        public override void Initialize()
        {
            // we have to initialize theme manager as soon as possible in order to correctly apply the style
            var themeManager = new ThemeManager(new ThemeSettingsProvider(CreateUserSettings()));
            var color = Application.Current!.Resources["AccentHue"];
            AvaloniaXamlLoader.Load(this); // this call loses the color in Resources :shrug:
            Application.Current!.Resources["AccentHue"] = color;
            Application.Current!.Resources["ClassicBorderBrush"] = ClassicBorderDecorator.ClassicBorderBrush;
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
        public MainThread()
        {
            Profiler.SetupMainThread(this);
        }
        
        public System.IDisposable Delay(Action action, TimeSpan delay)
        {
            return DispatcherTimer.RunOnce(action, delay);
        }

        public void Dispatch(Action action)
        {
            Dispatcher.UIThread.Post(action);
        }

        public Task Dispatch(Func<Task> action)
        {
            var tcs = new TaskCompletionSource();
            Dispatcher.UIThread.Post(() => Do(action, tcs).ListenErrors());
            return tcs.Task;
        }

        public IDisposable StartTimer(Func<bool> action, TimeSpan interval)
        {
            return DispatcherTimer.Run(action, interval);
        }

        private async Task Do(Func<Task> action, TaskCompletionSource tcs)
        {
            try
            {
                await action();
                tcs.SetResult();
            }
            catch (Exception)
            {
                tcs.SetResult();
                throw;
            }
        }
    }
}
