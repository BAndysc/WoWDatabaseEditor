using System.Threading;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Prism.Events;
using Prism.Ioc;
using RenderingTester;
using TheEngine;
using Unity;
using WDE.AzerothCore;
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

namespace AvaloniaRenderingTester
{
    public partial class MainWindow : Avalonia.Controls.Window
    {
        public IGame Game { get; }
        public MainWindow()
        {
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
            var mainThread = new MainThread(null!);
            registry.RegisterInstance<IMainThread>(mainThread);
            registry.RegisterInstance<IEventAggregator>(new EventAggregator());
            new MpqModule().RegisterTypes(registry);
            new WoWDatabaseEditorCore.MainModule().RegisterTypes(registry);
            new TrinityMySqlDatabaseModule().RegisterTypes(registry);
            new ParametersModule().RegisterTypes(registry);
            new TrinityModule().RegisterTypes(registry);
            new AzerothModule().RegisterTypes(registry);
            Game = provider.Resolve<Game>();
            
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }
    }
}