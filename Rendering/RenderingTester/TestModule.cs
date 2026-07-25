using System.Collections;
using TheEngine.Interfaces;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MpqReader.DBC;
using Hexa.NET.ImGui;
using TheEngine.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Structures;
using TheEngine.Utils;
using TheMaths;
using WDE.MapRenderer.Utils;
using WDE.MpqReader.Structures;
using Plane = TheMaths.Plane;

public class TestModule : IGameModule
{
    private readonly Engine engine;
    private readonly IEntityManager entityManager;
    private readonly Archetypes archetypes;
    private readonly IRenderManager renderManager;
    private readonly IGameContext gameContext;
    private readonly CreatureDisplayInfoStore displayInfoStore;
    private readonly CreatureModelDataStore modelDataStore;
    private readonly ICameraManager cameraManager;

    public TestModule(Engine engine,
        IEntityManager entityManager,
        Archetypes archetypes,
        IRenderManager renderManager,
        IGameContext gameContext,
        CreatureDisplayInfoStore displayInfoStore,
        CreatureModelDataStore modelDataStore,
        ICameraManager cameraManager)
    {
        this.engine = engine;
        this.entityManager = entityManager;
        this.archetypes = archetypes;
        this.renderManager = renderManager;
        this.gameContext = gameContext;
        this.displayInfoStore = displayInfoStore;
        this.modelDataStore = modelDataStore;
        this.cameraManager = cameraManager;
    }

    public void Dispose()
    {
    }

    public object? ViewModel { get; set; }

    private CreatureInstance? creatureInstance;
    private GameObjectInstance? gameObjectInstance;
    private int selectedDisplayInfoIndex = -1;
    private int lastSelectedAnimationId = 0;
    // Add a field to track last model index for animation reset
    private int lastModelDisplayInfoIndex = -1;
    private string displayInfoSearch = string.Empty;
    private string lastDisplayInfoSearch = string.Empty;
    private List<int>? filteredDisplayInfoIndices = null;

    public void Initialize()
    {
    }

    public void Render(float delta)
    {
        if (pp != null && creatureInstance is {} ci && ci.WorldObjectEntity != Entity.Empty)
        {
            pp.Render([ci.WorldObjectEntity]);
        }
        // var camera = cameraManager.MainCamera;
        // var view = camera.ViewMatrix;
        // var projection = camera.ProjectionMatrix;
        //
        // // Calculate the visible grid area on Z=0 plane
        // // 1. Get frustum corners in NDC
        // System.Numerics.Vector3[] ndcCorners = new System.Numerics.Vector3[]
        // {
        //     new System.Numerics.Vector3(-1, -1, 0), // bottom left
        //     new System.Numerics.Vector3(1, -1, 0),  // bottom right
        //     new System.Numerics.Vector3(1, 1, 0),   // top right
        //     new System.Numerics.Vector3(-1, 1, 0),  // top left
        // };
        //
        // // 2. Unproject to world space at Z = 0
        // var viewProj = view * projection;
        // System.Numerics.Matrix4x4.Invert(viewProj, out var invViewProj);
        //
        // var plane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
        //
        // Ray[] rays =
        // [
        //     camera.NormalizedScreenPointToRay(new Vector2(0, 0)),
        //     camera.NormalizedScreenPointToRay(new Vector2(0, 1)),
        //     camera.NormalizedScreenPointToRay(new Vector2(1, 0)),
        //     camera.NormalizedScreenPointToRay(new Vector2(1, 1))
        // ];
        //
        // rays[0].Intersects(ref plane, out Vector3 p1);
        // rays[1].Intersects(ref plane, out Vector3 p2);
        // rays[2].Intersects(ref plane, out Vector3 p3);
        // rays[3].Intersects(ref plane, out Vector3 p4);
        //
        // Vector3[] worldCorners =
        // [
        //     p1, p2, p3, p4,
        //     new Vector3(camera.Transform.Position.X - 5000, camera.Transform.Position.Y - 5000, 0),
        //     new Vector3(camera.Transform.Position.X + 5000, camera.Transform.Position.Y - 5000, 0),
        //     new Vector3(camera.Transform.Position.X + 5000, camera.Transform.Position.Y + 5000, 0),
        //     new Vector3(camera.Transform.Position.X - 5000, camera.Transform.Position.Y + 5000, 0)
        // ];
        //
        // float minX = worldCorners.Min(c => c.X);
        // float maxX = worldCorners.Max(c => c.X);
        // float minY = worldCorners.Min(c => c.Y);
        // float maxY = worldCorners.Max(c => c.Y);
        //
        // int startX = (int)MathF.Floor(minX);
        // int endX = (int)MathF.Ceiling(maxX);
        // int startY = (int)MathF.Floor(minY);
        // int endY = (int)MathF.Ceiling(maxY);
        //
        // // Draw vertical lines (constant X)
        // for (int x = startX; x <= endX; ++x)
        // {
        //     var start = new System.Numerics.Vector3(x, minY, 0);
        //     var end = new System.Numerics.Vector3(x, maxY, 0);
        //     gameContext.Engine.renderManager.DrawLine(start, end, System.Numerics.Vector4.One);
        // }
        //
        // // Draw horizontal lines (constant Y)
        // for (int y = startY; y <= endY; ++y)
        // {
        //     var start = new System.Numerics.Vector3(minX, y, 0);
        //     var end = new System.Numerics.Vector3(maxX, y, 0);
        //     gameContext.Engine.renderManager.DrawLine(start, end, System.Numerics.Vector4.One);
        // }
    }


    public unsafe void RenderGUI()
    {
        ImGui.Begin("Creature Display Infos"u8);
        ImGui.InputText("Search"u8, ref displayInfoSearch, 256);
        bool searchChanged = !string.Equals(displayInfoSearch, lastDisplayInfoSearch, StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(displayInfoSearch))
        {
            if (filteredDisplayInfoIndices == null || searchChanged)
            {
                filteredDisplayInfoIndices = new List<int>();
                for (int i = 0; i < displayInfoStore.Count; ++i)
                {
                    var displayInfo = displayInfoStore.ElementAt(i);
                    bool matches = false;
                    // Search by display id
                    if (displayInfo.Id.ToString().Contains(displayInfoSearch, StringComparison.OrdinalIgnoreCase))
                        matches = true;
                    // Search by model name
                    else if (modelDataStore.TryGetValue((uint)displayInfo.ModelId, out var modelData) &&
                        modelData->ModelName.ToString().Contains(displayInfoSearch, StringComparison.OrdinalIgnoreCase))
                        matches = true;
                    if (matches)
                        filteredDisplayInfoIndices.Add(i);
                }
                lastDisplayInfoSearch = displayInfoSearch;
            }
        }
        else
        {
            filteredDisplayInfoIndices = null;
            lastDisplayInfoSearch = string.Empty;
        }
        int displayCount = filteredDisplayInfoIndices?.Count ?? displayInfoStore.Count;
        if (ImGui.BeginListBox("##displayInfos"u8, new System.Numerics.Vector2(400, 400)))
        {
            ImGuiListClipper clipper = new ImGuiListClipper();
            clipper.Begin(displayCount, ImGui.GetTextLineHeightWithSpacing());
            while (clipper.Step())
            {
                for (int idx = clipper.DisplayStart; idx < clipper.DisplayEnd; ++idx)
                {
                    int i = filteredDisplayInfoIndices != null ? filteredDisplayInfoIndices[idx] : idx;
                    var displayInfo = displayInfoStore.ElementAt(i);
                    string modelName = "Unknown";
                    if (modelDataStore.TryGetValue((uint)displayInfo.ModelId, out var modelData))
                        modelName = modelData->ModelName.ToString();
                    string label = $"{displayInfo.Id}: {modelName} (ModelId: {displayInfo.ModelId})";
                    bool isSelected = selectedDisplayInfoIndex == i;
                    if (ImGui.Selectable(label, isSelected))
                    {
                        if (selectedDisplayInfoIndex != i)
                        {
                            selectedDisplayInfoIndex = i;
                            creatureInstance?.Dispose();
                            creatureInstance = new CreatureInstance(gameContext, "Name", (uint)displayInfo.Id, RenderLayer.Default);
                            creatureInstance.Load().FireAndForget();
                            // Reset animation selection on new model
                            lastModelDisplayInfoIndex = -1;
                        }
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
            }
            clipper.End();
            ImGui.EndListBox();
        }
        ImGui.End(); // End Creature Display Infos window

        // Animation Combo Box
        if (creatureInstance != null && creatureInstance.Model != null && creatureInstance.Model.sequenceIdToAnimationLookup != null)
        {
            var lookup = creatureInstance.Model.sequenceIdToAnimationLookup;
            List<int> validAnimationIds = new List<int>();
            List<string> validAnimationNames = new List<string>();
            for (int animId = 0; animId < lookup.Length; ++animId)
            {
                if (lookup[animId] != -1)
                {
                    validAnimationIds.Add(animId);
                    var name = System.Enum.GetName(typeof(WDE.MpqReader.Structures.M2AnimationType), animId) ?? $"Anim {animId}";
                    validAnimationNames.Add(name);
                }
            }
            // Remember last selected animation id, reset if model changed
            if (selectedDisplayInfoIndex != lastModelDisplayInfoIndex)
            {
                lastSelectedAnimationId = validAnimationIds.Count > 0 ? validAnimationIds[0] : 0;
                lastModelDisplayInfoIndex = selectedDisplayInfoIndex;
            }
            int animIndex = validAnimationIds.IndexOf(lastSelectedAnimationId);
            if (animIndex < 0) animIndex = 0;
            if (ImGui.Combo("Animation"u8, ref animIndex, validAnimationNames.ToArray(), validAnimationNames.Count))
            {
                lastSelectedAnimationId = validAnimationIds[animIndex];
                creatureInstance.Animation = (M2AnimationType)lastSelectedAnimationId;
            }
        }
    }

    private async ValueTask LoadCreatureAndModel(GameObjectInstance instance)
    {
        await instance.Load();
//         await instance.LoadMount(17694);
    }

    HighlightPostProcess? pp;
    private async ValueTask LoadCreatureAndModel(CreatureInstance instance)
    {
        await instance.Load();
//         await instance.LoadMount(17694);
        pp = new HighlightPostProcess(engine, Color.AliceBlue);
        engine.RenderManager.AddPostprocess(pp);
    }


    public void Update(float delta)
    {
        // if (gameObjectInstance == null)
        // {
        //     gameObjectInstance = new GameObjectInstance(gameContext, null, 474, RenderLayer.Default);
        //     LoadCreatureAndModel(gameObjectInstance).FireAndForget();
        // }

        if (creatureInstance == null)
        {
            creatureInstance = new CreatureInstance(gameContext,"aa", 13, RenderLayer.Default);
            LoadCreatureAndModel(creatureInstance).FireAndForget();
        }
        if (creatureInstance != null && creatureInstance.WorldObjectEntity != Entity.Empty)
        {
            // gameContext.Engine.EntityInspector.InspectEntity(creatureInstance.WorldObjectEntity);
        }

        if (!loadAllModels)
        {
            // LoadAllModels().FireAndForget();
            // loadAllModels = true;
        }
        // if (!waterfallLoaded)
        // {
        //     waterfallLoaded = true;
        //     gameContext.StartCoroutine(LoadWaterfall());
        // }
    }

    public async ValueTask LoadAllModels()
    {
        float x = 0;
        float y = 0;
        int index = 0;
        float max_y = 0;
        float sum_y = 0;
        HashSet<int> uniqueModels = new HashSet<int>();
        foreach (var disp in gameContext.DbcManager.CreatureDisplayInfoStore)
        {
            uint dispId;
            unsafe
            {
                dispId = disp->Id;
                if (!uniqueModels.Add(disp->ModelId))
                    continue;
            }
            creatureInstance = new CreatureInstance(gameContext, "", dispId, RenderLayer.Default);
            await creatureInstance.Load();
            if (creatureInstance.Model == null)
                continue;

            var scale=engine.entityManager.GetComponent<LocalToWorld>(creatureInstance.WorldObjectEntity).Scale;

            var boundingBox = (creatureInstance.Model.bounding_box.max - creatureInstance.Model.bounding_box.min) *
                              scale;
            x += Math.Min(boundingBox.X, 15);
            max_y = Math.Min(max_y, boundingBox.Y);
            sum_y += boundingBox.Y;
            creatureInstance.Position = new Vector3(x, y, 0);
            index++;
            if (index == 100)
            {
                index = 0;
                y += Math.Min(sum_y / 100, 15);
                sum_y = 0;
                max_y = 0;
                x = 0;
            }

            await engine.NextFrame;
        }
    }

    private bool loadAllModels = false;
    private bool waterfallLoaded = false;

    private async ValueTask LoadWaterfall()
    {
        var m = await gameContext.MdxManager.LoadM2Mesh(new FileId(196342));

        Entity entity = Entity.Empty;
        int wfBoneBase = 0, wfColorBase = 0, wfTexBase = 0;
        if (m.HasAnimations)
        {
            (wfBoneBase, wfColorBase, wfTexBase) = gameContext.AnimationSystem.AllocateAnimationSlots(m.model);
        }

        bool first = true;

        foreach (var material in m.materials)
        {
            if (m.HasAnimations)
            {
                if (first)
                {
                    entity = entityManager.CreateEntity(archetypes.StaticM2WorldObjectAnimatedArchetype, "Waterfall");
                    entityManager.SetManagedComponent(entity, new M2AnimationComponentData(m.model)
                    {
                        SetNewAnimation = 0,
                        BoneBase = wfBoneBase,
                        ColorBase = wfColorBase,
                        TexTransformBase = wfTexBase,
                        _boneCache = AnimationSystem.IdentityMatrix(m.model.bones.Length).ToArray(),
                        _colorCache = AnimationSystem.IdentityColors(m.model.colors.Length).ToArray(),
                        _texTransformCache = AnimationSystem.IdentityMatrix(m.model.texture_transforms.Length + 1).ToArray(),
                    });
                }
            }
            else
            {
                if (first)
                    entity = entityManager.CreateEntity(archetypes.StaticM2WorldObjectArchetype, "waterfall");
            }

            var t = new Transform();
            var instanceData = new Int4(
                material.batch.colorIndex < 0 ? -1 : wfColorBase + material.batch.colorIndex,
                material.batch.textureTransformIndex < 0 ? -1 : wfTexBase + material.batch.textureTransformIndex,
                material.batch.textureTransformIndex2 < 0 ? -1 : wfTexBase + material.batch.textureTransformIndex2,
                wfBoneBase);
            renderManager.SetupRendererEntity(entity, m.mesh.Handle, material.material, material.submesh, t.LocalToWorldMatrix, instanceData);
            entityManager.AddManagedComponent(entity, new MdxRenderer(m) { Owner = entity });
            first = false;
        }
    }
}