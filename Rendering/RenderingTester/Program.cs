using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using RenderingTester;
using TheEngine;
using TheEngine.Utils;
using TheMaths;
using Unity;
using WDE.AzerothCore;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.TableData;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.DbcStore;
using WDE.DbcStore.FastReader;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns;
using WDE.MapSpawns.Rendering;
using WDE.Module;
using WDE.MPQ;
using WDE.Parameters;
using WDE.QueryGenerators;
using WDE.SqlInterpreter;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;
using WDE.WorldMap.Services;
using WoWDatabaseEditorCore;
using WoWDatabaseEditorCore.Services.LoadingEvents;

// the line below is only used to satisfy assertion
// SynchronizationContext.Current != null
// at WoWDatabaseEditorCore.Tasks.TaskRunner.AssertMainThread() in WoWDatabaseEditor/Tasks/TaskRunner.cs:line 51
// GameRunner will override the SynchronizationContext with the engine SC anyway
// AND EventAggregator
var customSc = new CustomSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(customSc);

var nativeWindowSettings = new NativeWindowSettings()
{
    Size = new Vector2i(1280, 720),
    Title = "WoW Database Editor - 3D Debug view",
    // This is needed to run on macos
    API = ContextAPI.NoAPI
};

var container = new UnityContainer();
DI.Container = container;
container.AddExtension(new Diagnostic());
var extensions = new UnityContainerExtension(container);
var scopedContainer = new ScopedContainer(new UnityContainerExtension(container), new UnityContainerRegistry(container, extensions), container);
var registry = new UnityContainerRegistry(container, extensions);
var provider = new UnityContainerProvider(container);

registry.RegisterInstance<IScopedContainer>(scopedContainer);
registry.RegisterInstance<IContainerProvider>(provider);
registry.RegisterInstance<IContainerRegistry>(registry);

var gameProperties = new DummyGameProperties();
// var gameView = new DummyGameView();
registry.Register<ILoadingEventAggregator, LoadingEventAggregator>();
// registry.RegisterInstance<IGameView>(gameView);
registry.Register<IStatusBar, DummyStatusBar>();
registry.RegisterInstance<IGameProperties>(gameProperties);
registry.Register<IMessageBoxService, DummyMessageBox>();
registry.Register<IDatabaseClientFileOpener, DatabaseClientFileOpener>();
registry.Register<ITableEditorPickerService, DummyTableEditorPickerService>();
registry.Register<ITabularDataPicker, DummyTabularDataPicker>();
registry.Register<IWindowManager, DummyWindowManager>();
registry.Register<IDatabaseRowsCountProvider, DummyDatabaseRowsCountProvider>();
registry.Register<IQueryEvaluator, DummyQueryEvaluator>();
registry.Register<IViewLocator, DummyViewLocator>();
registry.Register<ICreatureEntryOrGuidProviderService, DummyCreatureEntryOrGuidProviderService>();
registry.Register<IRemoteConnectorService, DummyRemoteConnectorService>();
registry.Register<IQuestEntryProviderService, DummyQueryEntryProviderService>();
registry.Register<IMapDataProvider, DummyMapDataProvider>();
var mainThread = new MainThread();
GlobalApplication.InitializeApplication(mainThread, GlobalApplication.AppBackend.Avalonia);
registry.RegisterInstance<IMainThread>(mainThread);
registry.RegisterInstance<IEventAggregator>(new EventAggregator());

SetupModules(new DbcStoreModule(),
    new MpqModule(),
    new WoWDatabaseEditorCore.MainModule(),
    new QueryGeneratorModule(),
    new TrinityMySqlDatabaseModule(),
    new ParametersModule(),
    new TrinityModule(),
    new AzerothModule(),
    new MapSpawnsModule(),
    new MapRendererModule());

void SetupModules(params ModuleBase[] modules)
{
    foreach (var module in modules)
    {
        // matches the TrinityModule + TrinityMySqlDatabaseModule loaded above; a real core (not
        // "unspecified") is required for the RequiresCore-gated SQL providers (creature_formations,
        // spawn_group, ...) to register, which the formation/spawn-group editor tools need.
        module.InitializeCore("TrinityMaster");
        module.RegisterTypes(registry);
        module.OnInitialized(registry);
    }
}

var game = provider.Resolve<Game>();
var gameView = provider.Resolve<IGameView>();
gameView.RegisterGameModule(_ => mainThread);
gameView.RegisterGameModule(_ => customSc);
//gameView.AddModule<TestModule>();
gameProperties.LoadWorld = true;
gameProperties.DisableTimeFlow = true; // keep noon lighting for repeatable visual checks

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Console.WriteLine(args.ExceptionObject);
};

provider.Resolve<IEventAggregator>()
    .GetEvent<AllModulesLoaded>()
    .Publish();

var gameViewModel = provider.Resolve<GameViewModel>();

using var window = new GameStandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, game, mainThread);
registry.RegisterInstance<IClipboardService>(window);
window.Run();
TheEngine.TheEngine.Deinit();