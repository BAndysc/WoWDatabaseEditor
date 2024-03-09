using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using Prism.Ioc;
using Unity;
using Unity.Extension;
using Unity.Resolution;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.TableData;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns;
using WDE.MapSpawns.Rendering;
using WDE.Module;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;
using WDE.SqlInterpreter;

namespace RenderingTester;

public class DummyStatusBar : IStatusBar
{
    public void PublishNotification(INotification notification)
    {
    }

    public INotification? CurrentNotification { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class DummyGameView : IGameView
{
    public IEnumerable<Func<IContainerProvider, IGameModule>> Modules { get; } =
        new List<Func<IContainerProvider, IGameModule>>()
        {
            provider => provider.Resolve<StandaloneCustomGameModule>(),
            provider => provider.Resolve<DebugWindow>(),
            provider => provider.Resolve<SpawnViewer>()
        };
    public event Action<Func<IContainerProvider, IGameModule>>? ModuleRegistered;
    public event Action<Func<IContainerProvider, IGameModule>>? ModuleRemoved;
    public IDisposable RegisterGameModule(Func<IContainerProvider, IGameModule> gameModule) => Disposable.Empty;

    public Task<Game> Open()
    {
        return Task.FromResult<Game>(null!);
    }
}

public class DummyGameProperties : IGameProperties
{
    public bool OverrideLighting { get; set; } = false;
    public bool DisableTimeFlow { get; set; } = false;
    public int TimeSpeedMultiplier { get; set; } = 2;
    public bool ShowGrid { get; set; } = false;
    public Time CurrentTime { get; set; } = Time.FromMinutes(720);
    public float ViewDistanceModifier { get; set; } = 16;
    public bool ShowAreaTriggers { get; set; } = false;
    public int TextureQuality { get; set; } = 3;
    public float DynamicResolution { get; set; } = 1;
    public bool RenderGui { get; set; } = true;
}

public class DummyMessageBox : IMessageBoxService
{
    public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox)
    {
        Console.WriteLine(messageBox.MainInstruction);
        Console.WriteLine(messageBox.Content);
        return Task.FromResult<T?>(default);
    }
}

public class MainThread : IMainThread
{
    private readonly SingleThreadSynchronizationContext context;
    private List<(TimeSpan delay, Action action, long delayId)> delayed = new();
    private long delayId = 0;

    public MainThread(SingleThreadSynchronizationContext context)
    {
        this.context = context;
    }

    public System.IDisposable Delay(Action action, TimeSpan delay)
    {
        var id = delayId;
        delayed.Add((delay, action, id));
        delayId++;
        return new ActionDisposable(() =>
        {
            delayed.RemoveIf(x => x.delayId == delayId);
        });
    }

    public void Dispatch(Action action)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        action();
    }

    public Task Dispatch(Func<Task> action)
    {
        return action();
    }

    public IDisposable StartTimer(Func<bool> action, TimeSpan interval)
    {
        throw new Exception("Operation not supported, but could be implemented");
    }

    public void Tick(TimeSpan time)
    {
        for (int i = 0; i < delayed.Count; ++i)
        {
            if (delayed[i].delay <= time)
            {
                var action = delayed[i].action;
                if (i != delayed.Count - 1)
                {
                    delayed[i] = delayed[^1];
                    --i;
                }
                delayed.RemoveAt(delayed.Count - 1);
                
                action();
            }
            else
            {
                var newDelay = delayed[i].delay - time;
                delayed[i] = (newDelay, delayed[i].action, delayed[i].delayId);
            }
        }
    }
}

public class UnityContainerExtension : IContainerExtension<IUnityContainer>
{
    public IUnityContainer Instance { get; }

    public UnityContainerExtension() : this(new UnityContainer())
    {
    }

    public UnityContainerExtension(IUnityContainer container) => Instance = container;

    public void FinalizeExtension()
    {
    }

    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        Instance.RegisterInstance(type, instance);
        return this;
    }

    public IContainerRegistry RegisterInstance(Type type, object instance, string name)
    {
        Instance.RegisterInstance(type, name, instance);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to)
    {
        Instance.RegisterSingleton(from, to);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
    {
        Instance.RegisterSingleton(from, to, name);
        return this;
    }

    public IContainerRegistry Register(Type from, Type to)
    {
        Instance.RegisterType(from, to);
        return this;
    }

    public IContainerRegistry Register(Type from, Type to, string name)
    {
        Instance.RegisterType(from, to, name);
        return this;
    }

    public object Resolve(Type type)
    {
        return Instance.Resolve(type);
    }

    public object Resolve(Type type, string name)
    {
        return Instance.Resolve(type, name);
    }

    public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
    {
        var overrides = parameters.Select(p => new DependencyOverride(p.Type, p.Instance)).ToArray();
        return Instance.Resolve(type, overrides);
    }

    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
    {
        var overrides = parameters.Select(p => new DependencyOverride(p.Type, p.Instance)).ToArray();
        return Instance.Resolve(type, name, overrides);
    }

    public bool IsRegistered(Type type)
    {
        return Instance.IsRegistered(type);
    }

    public bool IsRegistered(Type type, string name)
    {
        return Instance.IsRegistered(type, name);
    }
}

public class ScopedContainer : BaseScopedContainer
{
    public override IScopedContainer CreateScope()
    {
        var childContainer = unity.CreateChildContainer();
        var lt = new DefaultLifetime();
        childContainer.AddExtension(lt);
        lt.TypeDefaultLifetime = new ContainerControlledLifetimeManager();
        var extensions = new UnityContainerExtension(childContainer);
        var scope = new ScopedContainer(extensions, new UnityContainerRegistry(childContainer, extensions), childContainer);
        extensions.RegisterInstance<IScopedContainer>(scope);
        extensions.RegisterInstance<IContainerExtension>(scope);
        extensions.RegisterInstance<IContainerProvider>(scope);
        extensions.RegisterInstance<IContainerRegistry>(scope);
        return scope;
    }

    public ScopedContainer(IContainerProvider provider, IContainerRegistry registry, IUnityContainer impl) : base(provider, registry, impl)
    {
    }
}

public class DummyQueryEvaluator : IQueryEvaluator
{
    public IEnumerable<InsertQuery> ExtractInserts(string query) => Enumerable.Empty<InsertQuery>();

    public IEnumerable<UpdateQuery> ExtractUpdates(string query) => Enumerable.Empty<UpdateQuery>();

    public IReadOnlyList<IBaseQuery> Extract(string query) => Array.Empty<IBaseQuery>();
}

public class DummyTableEditorPickerService : ITableEditorPickerService
{
    public async Task<long?> PickByColumn(DatabaseTable table, DatabaseKey? key, string column, long? initialValue, string? backupColumn = null, string? customWhere = null)
    {
        Console.WriteLine("Objects editing not supported in standalone window");
        return null;
    }

    public Task ShowTable(DatabaseTable table, string? condition, DatabaseKey? defaultPartialKey = null)
    {
        Console.WriteLine("Objects editing not supported in standalone window");
        return Task.CompletedTask;
    }

    public Task ShowForeignKey1To1(DatabaseTable table, DatabaseKey key)
    {
        Console.WriteLine("Objects editing not supported in standalone window");
        return Task.CompletedTask;
    }
}

public class DummyTabularDataPicker : ITabularDataPicker
{
    public async Task<T?> PickRow<T>(ITabularDataArgs<T> args, int defaultSelection = -1, string? defaultSearchText = null) where T : class
    {
        return default(T?);
    }

    public async Task<IReadOnlyCollection<T>?> PickRows<T>(ITabularDataArgs<T> args, IReadOnlyList<int>? defaultSelection = null, string? defaultSearchText = null,
        bool useCheckBoxes = false) where T : class
    {
        return null;
    }
}

public class DummyWindowManager : IWindowManager
{
    public IAbstractWindowView ShowWindow(IWindowViewModel viewModel, out Task task)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ShowDialog(IDialog viewModel)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ShowDialog(IDialog viewModel, out IAbstractWindowView? window)
    {
        window = null;
        throw new NotImplementedException();
    }

    public IAbstractWindowView ShowStandaloneDocument(IDocument document, out Task task)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> ShowFolderPickerDialog(string defaultDirectory)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> ShowOpenFileDialog(string filter, string? defaultDirectory = null)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> ShowSaveFileDialog(string filter, string? defaultDirectory = null, string? initialFileName = null)
    {
        throw new NotImplementedException();
    }

    public void OpenUrl(string url, string arguments = "")
    {
        throw new NotImplementedException();
    }

    public void Activate()
    {
        throw new NotImplementedException();
    }

    public async Task OpenPopupMenu<T>(T menuViewModel, object? control) where T : IPopupMenuViewModel
    {
        throw new NotImplementedException();
    }
}

public class DummyDatabaseRowsCountProvider : IDatabaseRowsCountProvider
{
    public async Task<int> GetRowsCountByPrimaryKey(DatabaseTable table, long primaryKey, CancellationToken token)
    {
        return 0;
    }

    public async Task<int> GetCreaturesCountByEntry(long entry, CancellationToken token)
    {
        return 0;
    }

    public async Task<int> GetGameObjectCountByEntry(long entry, CancellationToken token)
    {
        return 0;
    }
}
