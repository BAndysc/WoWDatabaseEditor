using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using RenderingTester;
using TheEngine;
using TheMaths;
using Unity;
using WDE.AzerothCore;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.TableData;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore;
using WDE.DbcStore.FastReader;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns;
using WDE.Module;
using WDE.MPQ;
using WDE.Parameters;
using WDE.QueryGenerators;
using WDE.SqlInterpreter;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;
using WoWDatabaseEditorCore;

var nativeWindowSettings = new NativeWindowSettings()
{
    Size = new Vector2i(1280, 720),
    Title = "WoW Database Editor - 3D Debug view",
    // This is needed to run on macos
    Flags = ContextFlags.ForwardCompatible,
};

var container = new UnityContainer();
container.AddExtension(new Diagnostic());
var extensions = new UnityContainerExtension(container);
var scopedContainer = new ScopedContainer(new UnityContainerExtension(container), new UnityContainerRegistry(container, extensions), container);
var registry = new UnityContainerRegistry(container, extensions);
var provider = new UnityContainerProvider(container);

registry.RegisterInstance<IScopedContainer>(scopedContainer);
registry.RegisterInstance<IContainerProvider>(provider);
registry.RegisterInstance<IContainerRegistry>(registry);

registry.Register<IGameView, DummyGameView>();
registry.Register<IStatusBar, DummyStatusBar>();
registry.Register<IGameProperties, DummyGameProperties>();
registry.Register<IMessageBoxService, DummyMessageBox>();
registry.Register<IDatabaseClientFileOpener, DatabaseClientFileOpener>();
registry.Register<ITableEditorPickerService, DummyTableEditorPickerService>();
registry.Register<ITabularDataPicker, DummyTabularDataPicker>();
registry.Register<IWindowManager, DummyWindowManager>();
registry.Register<IDatabaseRowsCountProvider, DummyDatabaseRowsCountProvider>();
registry.Register<IQueryEvaluator, DummyQueryEvaluator>();
var context = new SingleThreadSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
var mainThread = new MainThread(context);
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
    new MapSpawnsModule());

void SetupModules(params ModuleBase[] modules)
{
    foreach (var module in modules)
    {
        module.InitializeCore("unspecified");
        module.RegisterTypes(registry);
    }
}

SynchronizationContext.SetSynchronizationContext(context);

var game = provider.Resolve<Game>();
using var window = new GameStandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, game, mainThread, context);
registry.RegisterInstance<IClipboardService>(window);
window.Run();
TheEngine.TheEngine.Deinit();