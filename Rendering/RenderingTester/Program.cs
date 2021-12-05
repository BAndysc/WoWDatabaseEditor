using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Prism.Events;
using Prism.Ioc;
using RenderingTester;
using TheEngine;
using Unity;using WDE.AzerothCore;
using WDE.Common.CoreVersion;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.FastReader;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MPQ;
using WDE.Parameters;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;

var nativeWindowSettings = new NativeWindowSettings()
{
    Size = new Vector2i(1280, 720),
    Title = "WoW Database Editor - 3D Debug view",
    // This is needed to run on macos
    Flags = ContextFlags.ForwardCompatible,
};

var container = new UnityContainer();
container.AddExtension(new Diagnostic());
var registry = new UnityContainerRegistry(container);
var provider = new UnityContainerProvider(container);

registry.RegisterInstance<IContainerProvider>(provider);
registry.RegisterInstance<IContainerRegistry>(registry);

registry.Register<IGameView, DummyGameView>();
registry.Register<IStatusBar, DummyStatusBar>();
registry.Register<IGameProperties, DummyGameProperties>();
registry.Register<IMessageBoxService, DummyMessageBox>();
registry.Register<IDatabaseClientFileOpener, DatabaseClientFileOpener>();
registry.Register<IMainThread, MainThread>();
registry.RegisterInstance<IEventAggregator>(new EventAggregator());
new MpqModule().RegisterTypes(registry);
new WoWDatabaseEditorCore.MainModule().RegisterTypes(registry);
new TrinityMySqlDatabaseModule().RegisterTypes(registry);
new ParametersModule().RegisterTypes(registry);
new TrinityModule().RegisterTypes(registry);
new AzerothModule().RegisterTypes(registry);

var game = provider.Resolve<Game>();
using var window = new TheEngineOpenTkWindow(GameWindowSettings.Default, nativeWindowSettings, game);
window.Run();