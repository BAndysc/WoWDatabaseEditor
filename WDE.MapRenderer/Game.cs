using System.Windows.Input;
using Prism.Ioc;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using Unity;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.Module.Attributes;

namespace WDE.MapRenderer;

[AutoRegister]
public class Game : IGame
{
    private readonly IMpqService mpqService;
    private readonly IGameView gameView;
    private readonly IGameProperties gameProperties;
    private readonly IMessageBoxService messageBoxService;
    private readonly IDatabaseClientFileOpener databaseClientFileOpener;
    private readonly IDatabaseProvider databaseProvider;
    private GameManager? manager;
    public GameManager? Manager => manager; 
        
    private TaskCompletionSource<bool> waitForInitialized = new();
    public Task<bool> WaitForInitialized => waitForInitialized.Task;
    public event Action? OnInitialized;
    public event Action? OnFailedInitialize;
    
    public Game(IMpqService mpqService, 
        IGameView gameView,
        IGameProperties gameProperties,
        IMessageBoxService messageBoxService,
        IDatabaseClientFileOpener databaseClientFileOpener,
        IDatabaseProvider databaseProvider)
    {
        this.mpqService = mpqService;
        this.gameView = gameView;
        this.gameProperties = gameProperties;
        this.messageBoxService = messageBoxService;
        this.databaseClientFileOpener = databaseClientFileOpener;
        this.databaseProvider = databaseProvider;
    }
        
    public bool Initialize(Engine engine)
    {
        var unity = new UnityContainer();
        unity.AddExtension(new Diagnostic());
        var provider = new UnityContainerProvider(unity);
        var registry = new UnityContainerRegistry(unity);

        registry.RegisterInstance(typeof(IGameProperties), gameProperties);
        registry.RegisterInstance(typeof(Engine), engine);
        registry.RegisterInstance(typeof(IUIManager), engine.Ui);
        registry.RegisterInstance(typeof(ICameraManager), engine.CameraManager);
        registry.RegisterInstance(typeof(IEntityManager), engine.EntityManager);
        registry.RegisterInstance(typeof(IFontManager), engine.FontManager);
        registry.RegisterInstance(typeof(IInputManager), engine.InputManager);
        registry.RegisterInstance(typeof(ILightManager), engine.LightManager);
        registry.RegisterInstance(typeof(IMaterialManager), engine.MaterialManager);
        registry.RegisterInstance(typeof(IMeshManager), engine.MeshManager);
        registry.RegisterInstance(typeof(IRenderManager), engine.RenderManager);
        registry.RegisterInstance(typeof(IShaderManager), engine.ShaderManager);
        registry.RegisterInstance(typeof(IStatsManager), engine.StatsManager);
        registry.RegisterInstance(typeof(ITextureManager), engine.TextureManager);
        registry.RegisterInstance(typeof(IContainerProvider), provider);
        registry.RegisterInstance(typeof(IContainerRegistry), registry);
        registry.RegisterInstance(typeof(IMpqService), mpqService);
        registry.RegisterInstance(typeof(IGameView), gameView);
        registry.RegisterInstance(typeof(IMessageBoxService), messageBoxService);
        registry.RegisterInstance(typeof(IDatabaseClientFileOpener), databaseClientFileOpener);
        registry.RegisterInstance(typeof(IDatabaseProvider), databaseProvider);
            
        registry.RegisterSingleton<NotificationsCenter>();
        registry.RegisterSingleton<TimeManager>();
        registry.RegisterSingleton<ScreenSpaceSelector>();
        registry.RegisterSingleton<DbcManager>();
        registry.RegisterSingleton<LoadingManager>();
        registry.RegisterSingleton<WoWTextureManager>();
        registry.RegisterSingleton<WoWMeshManager>();
        registry.RegisterSingleton<MdxManager>();
        registry.RegisterSingleton<WmoManager>();
        registry.RegisterSingleton<WorldManager>();
        registry.RegisterSingleton<ChunkManager>();
        registry.RegisterSingleton<CameraManager>();
        registry.RegisterSingleton<LightingManager>();
        registry.RegisterSingleton<AreaTriggerManager>();
        registry.RegisterSingleton<RaycastSystem>();
        registry.RegisterSingleton<ModuleManager>();
        registry.RegisterSingleton<CreatureManager>();
        registry.RegisterSingleton<GameObjectManager>();
        registry.RegisterSingleton<CoroutineManager>();
        registry.RegisterSingleton<IGameFiles, GameFiles>();
        
        manager = (GameManager)provider.Resolve(typeof(GameManager));
        registry.RegisterInstance<IGameContext>(manager);
            
        bool success = manager.Initialize();
        if (!success)
        {
            manager.DisposeGame();
            manager = null;
            OnFailedInitialize?.Invoke();
            waitForInitialized.SetResult(false);
            waitForInitialized = new();
            return false;
        }

        OnInitialized?.Invoke();
        waitForInitialized.SetResult(true);

        return success;
    }

    public void Update(float diff)
    {
        manager?.Update(diff);
    }

    public void Render(float delta)
    {
        manager?.Render(delta);
    }

    public void RenderTransparent(float delta)
    {
        manager?.RenderTransparent(delta);
    }

    public void RenderGUI(float delta)
    {
        manager?.RenderGui(delta);
    }

    public event Action? RequestDispose;
    public event Action<Game>? OnAfterDisposed;
        
    public void DoDispose()
    {
        RequestDispose?.Invoke();
    }
        
    public void DisposeGame()
    {
        manager?.DisposeGame();
        OnAfterDisposed?.Invoke(this);
        waitForInitialized = new();
    }

    public T? Resolve<T>() where T : class
    {
        return manager?.ResolveInstance<T>();
    }

    public List<(string, ICommand, object?)>? GenerateContextMenu()
    {
        return manager?.GenerateContextMenu();
    }
}