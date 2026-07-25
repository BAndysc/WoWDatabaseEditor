using ImGuiNET;
using System.Collections;
using TheEngine;
using TheEngine.Interfaces;
using TheEngine.Utils;
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
    private readonly GlobalWorldMapObjectManager globalWorldMapObjectManager;
    private readonly LowDetailHeightMapManager lowDetailHeightMapManager;
    private readonly ZoneAreaManager zoneAreaManager;
    private readonly WorldManager worldManager;
    private readonly Engine engine;
    private readonly IGameProperties gameProperties;
    private int? currentLoadedMap;
    private LoadingToken? loadingToken;
    private SimpleBox loadingNotificationBox;
    
    public bool EssentialLoadingInProgress { get; private set; }
    
    public LoadingManager(IGameContext gameContext,
        IUIManager uiManager,
        ChunkManager chunkManager, 
        GlobalWorldMapObjectManager globalWorldMapObjectManager,
        LowDetailHeightMapManager lowDetailHeightMapManager,
        ZoneAreaManager zoneAreaManager,
        WorldManager worldManager,
        Engine engine,
        IGameProperties gameProperties)
    {
        this.gameContext = gameContext;
        this.uiManager = uiManager;
        this.chunkManager = chunkManager;
        this.globalWorldMapObjectManager = globalWorldMapObjectManager;
        this.lowDetailHeightMapManager = lowDetailHeightMapManager;
        this.zoneAreaManager = zoneAreaManager;
        this.worldManager = worldManager;
        this.engine = engine;
        this.gameProperties = gameProperties;

        this.loadingNotificationBox = new SimpleBox(engine, BoxPlacement.BottomCenter);
    }

    public void Update(float delta)
    {
        if (!gameProperties.LoadWorld)
        {
            return;
        }
        if (currentLoadedMap != gameContext.CurrentMap.Id)
        {
            currentLoadedMap = gameContext.CurrentMap.Id;
            var oldLoadingToken = loadingToken;
            loadingToken = new LoadingToken();
            LoadingCoroutine(currentLoadedMap.Value, oldLoadingToken, loadingToken).FireAndForget();
        }
    }

    private async ValueTask LoadingCoroutine(int map, LoadingToken? old, LoadingToken newToken)
    {
        EssentialLoadingInProgress = true;
        if (old != null)
        {
            old.Cancel();
            while (!old.Loaded)
                await engine.NextFrame; // wait for previous loading to finish
        }
        
        await globalWorldMapObjectManager.Unload();
        
        await chunkManager.UnloadAllChunks();

        lowDetailHeightMapManager.Unload();
        
        await zoneAreaManager.Load();
        
        await worldManager.LoadMap(newToken.CancellationToken);

        if (loadingToken == newToken)
            EssentialLoadingInProgress = false;

        lowDetailHeightMapManager.Load();
        
        await globalWorldMapObjectManager.Load();
        
        await worldManager.LoadOptionals(newToken.CancellationToken);

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