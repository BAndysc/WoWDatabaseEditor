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
    private readonly GlobalWorldMapObjectManager globalWorldMapObjectManager;
    private readonly LowDetailHeightMapManager lowDetailHeightMapManager;
    private readonly ZoneAreaManager zoneAreaManager;
    private readonly WoWTextureManager textureManager;
    private readonly ModuleManager moduleManager;
    private readonly MdxManager mdxManager;
    private readonly WmoManager wmoManager;
    private readonly WorldManager worldManager;
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
        WoWTextureManager textureManager,
        ModuleManager moduleManager,
        MdxManager mdxManager,
        WmoManager wmoManager,
        WorldManager worldManager)
    {
        this.gameContext = gameContext;
        this.uiManager = uiManager;
        this.chunkManager = chunkManager;
        this.globalWorldMapObjectManager = globalWorldMapObjectManager;
        this.lowDetailHeightMapManager = lowDetailHeightMapManager;
        this.zoneAreaManager = zoneAreaManager;
        this.textureManager = textureManager;
        this.moduleManager = moduleManager;
        this.mdxManager = mdxManager;
        this.wmoManager = wmoManager;
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

        lowDetailHeightMapManager.Unload();

        textureManager.UnloadAllTextures();

        wmoManager.UnloadAllMeshes();
        
        mdxManager.UnloadAllMeshes();
        
        yield return zoneAreaManager.Load();
        
        yield return worldManager.LoadMap(newToken.CancellationToken);

        if (worldManager.CurrentWdt == null) // map not loaded
        {
            newToken.MarkAsLoaded();
            if (loadingToken == newToken)
            {
                EssentialLoadingInProgress = false;
                loadingToken = null;
            }
            yield break;
        }
        
        yield return moduleManager.ForEach(x => x.LoadMap(map));
        
        if (loadingToken == newToken)
            EssentialLoadingInProgress = false;

        lowDetailHeightMapManager.Load();
        
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