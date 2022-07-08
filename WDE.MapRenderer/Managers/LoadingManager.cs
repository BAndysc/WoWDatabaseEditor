using ImGuiNET;
using System.Collections;
using TheEngine.Interfaces;
using TheEngine.Utils.ImGuiHelper;
using TheMaths;

namespace WDE.MapRenderer.Managers;

public class LoadingToken
{
    private CancellationTokenSource? cancellationSource = new();

    public bool Loaded => cancellationSource == null;
    
    public CancellationToken CancellationToken => cancellationSource?.Token ?? CancellationToken.None;

    public void MarkAsLoaded()
    {
        cancellationSource = null;
    }
    
    public bool IsCancellationRequested()
    {
        return cancellationSource?.IsCancellationRequested ?? false;
    }
    
    public void Cancel()
    {
        cancellationSource?.Cancel();
    }
}

public class LoadingManager : IDisposable
{
    private readonly IGameContext gameContext;
    private readonly IUIManager uiManager;
    private readonly ChunkManager chunkManager;
    private readonly CreatureManager creatureManager;
    private readonly GameObjectManager gameObjectManager;
    private readonly GlobalWorldMapObjectManager globalWorldMapObjectManager;
    private readonly WorldManager worldManager;
    private int? currentLoadedMap;
    private LoadingToken? loadingToken;
    private SimpleBox loadingNotificationBox;
    
    public bool EssentialLoadingInProgress { get; private set; }
    
    public LoadingManager(IGameContext gameContext,
        IUIManager uiManager,
        ChunkManager chunkManager, 
        CreatureManager creatureManager,
        GameObjectManager gameObjectManager,
        GlobalWorldMapObjectManager globalWorldMapObjectManager,
        WorldManager worldManager)
    {
        this.gameContext = gameContext;
        this.uiManager = uiManager;
        this.chunkManager = chunkManager;
        this.creatureManager = creatureManager;
        this.gameObjectManager = gameObjectManager;
        this.globalWorldMapObjectManager = globalWorldMapObjectManager;
        this.worldManager = worldManager;

        this.loadingNotificationBox = new SimpleBox(BoxPlacement.BottomCenter);
    }

    public void Update(float delta)
    {
        if (currentLoadedMap != gameContext.CurrentMap.Id)
        {
            currentLoadedMap = gameContext.CurrentMap.Id;
            var oldLoadingToken = loadingToken;
            loadingToken = new LoadingToken();
            gameContext.StartCoroutine(LoadingCoroutine(currentLoadedMap.Value, oldLoadingToken, loadingToken));
        }
    }

    private IEnumerator LoadingCoroutine(int map, LoadingToken? old, LoadingToken newToken)
    {
        EssentialLoadingInProgress = true;
        if (old != null)
        {
            old.Cancel();
            while (!old.Loaded)
                yield return null; // wait for previous loading to finish
        }
        
        yield return globalWorldMapObjectManager.Unload();
        
        yield return chunkManager.UnloadAllChunks();

        yield return worldManager.LoadMap(newToken.CancellationToken);

        yield return creatureManager.LoadEssentialData(newToken.CancellationToken);
        
        yield return gameObjectManager.LoadEssentialData(newToken.CancellationToken);
        
        if (loadingToken == newToken)
            EssentialLoadingInProgress = false;

        yield return globalWorldMapObjectManager.Load();
        
        yield return worldManager.LoadOptionals(newToken.CancellationToken);

        newToken.MarkAsLoaded();
        
        if (loadingToken == newToken)
            loadingToken = null;
    }

    public void Dispose()
    {
    }

    public void RenderGUI()
    {
        if (loadingToken != null)
        {
            string message = EssentialLoadingInProgress ? "Loading essential things" : "Loading less important things";
            loadingNotificationBox.Draw(message);
        }
    }
}