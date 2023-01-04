using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using RenderingTester;
using TheEngine;
using Unity;
using WDE.AzerothCore;
using WDE.Common.Avalonia.Utils;
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

namespace AvaloniaRenderingTester
{
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

    public partial class MainWindow : Avalonia.Controls.Window
    {
        public IGame Game { get; }
        public MainWindow()
        {
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
            //var context = new SingleThreadSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
            var mainThread = new MainThread();
            registry.RegisterInstance<IMainThread>(mainThread);
            registry.RegisterInstance<IEventAggregator>(new EventAggregator());

            registry.RegisterInstance<IClipboardService>(new AvaloniaClipboard());
            
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
            
            Game = provider.Resolve<Game>();
            
            DataContext = this;
            AvaloniaXamlLoader.Load(this);;
            this.AttachDevTools();
        }
    }

    public class AvaloniaClipboard : IClipboardService
    {
        public AvaloniaClipboard()
        {
        }

        private IClipboard clipboard => Application.Current?.GetTopLevel()?.Clipboard!;
        
        public Task<string?> GetText()
        {
            return clipboard.GetTextAsync();
        }

        public void SetText(string text)
        {
            clipboard.SetTextAsync(text).ListenErrors();
        }
    }
}

