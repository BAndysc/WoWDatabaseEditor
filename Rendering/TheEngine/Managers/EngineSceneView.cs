using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheEngine.Primitives;
using TheMaths;
using Veldrid;
using MouseButton = TheEngine.Input.MouseButton;

namespace TheEngine.Managers;

internal class EngineSceneView : BaseBaseView
{
    private readonly Engine engine;
    private float pitch;
    private float yaw = 26.9f + 90;
    public Vector3 position;
    private Quaternion rotation;
    private float currentSpeed = 0;
    private IMesh gridPlane;
    private Material material;
    private RaycastSystem raycastSystem;

    // Lazily created the first time the user adds a primitive from the scene-view context menu.
    private IMesh? primitiveCubeMesh;
    private IMesh? primitiveSphereMesh;
    private Material? primitiveMaterial;
    private Archetype? primitiveRendererArchetype;

    // Field order/types must match internalShaders/pbr.frag's MaterialData struct exactly (std430
    // layout, 16-byte aligned throughout - see the Vulkan material-data SSBO convention).
    private struct PrimitiveMaterialData_t
    {
        public Vector4 albedo;
        public Vector4 emissive;
        public float metallic;
        public float roughness;
        public float ao;
        public float pbrPad1;
    }

    private enum PrimitiveType
    {
        Cube,
        Sphere,
    }

    /// <summary>When true the scene view culls geometry against its OWN camera (showing what this
    /// viewpoint should render) instead of re-displaying the game camera's culled set. Toggled from
    /// the scene-view toolbar; read by <see cref="RenderManager"/> to build a separate draw set.</summary>
    public bool OwnCulling;

    // Editor scene-view gizmo icons (camera-facing billboards for lights / cameras / decals).
    private IMesh? iconQuad;
    private Material? iconMaterial;
    private Archetype? lightArchetype;
    private Archetype? decalArchetype;
    // reused each frame to batch every gizmo into one instanced draw
    private readonly List<Matrix> iconModels = new();
    private readonly List<Int4> iconDrawData = new();

    // Icon picking: each pickable icon writes GizmoPickIdBase + index into the object-picker buffer;
    // gizmoPickEntities maps that index back to the entity the icon represents. Rebuilt every frame in
    // DrawGizmoIcons, in lockstep with iconDrawData. A high, fixed base keeps these ids clear of the
    // ObjectDrawRenderStage mesh indices (small) and the decal range (DecalManager.PickIdBase = 1<<24).
    internal const uint GizmoPickIdBase = 1u << 25;
    private readonly List<Entity> gizmoPickEntities = new();

    /// <summary>Resolves a gizmo pick index (object-picker value - GizmoPickIdBase) back to its entity.</summary>
    internal Entity EntityAtGizmoPickIndex(int index) =>
        index >= 0 && index < gizmoPickEntities.Count ? gizmoPickEntities[index] : Entity.Empty;

    private const int IconLight = 0;
    private const int IconCamera = 1;
    private const int IconDecal = 2;

    // Packed RGBA8 tints (see PackColor); alpha keeps the icons slightly translucent.
    private static readonly int LightColor = PackColor(1.0f, 0.85f, 0.2f, 0.85f);
    private static readonly int CameraColor = PackColor(0.85f, 0.9f, 1.0f, 0.85f);
    private static readonly int DecalColor = PackColor(0.4f, 0.85f, 1.0f, 0.85f);

    private DebugViewToolbar debugViewToolbar;
    
    public EngineSceneView(Engine engine)
    {
        this.engine = engine;
        raycastSystem = new RaycastSystem(engine);
        debugViewToolbar = new DebugViewToolbar();
    }

    // The world position a newly-added entity should be placed at: under the cursor if it's hovering
    // existing geometry, otherwise a fixed distance along the view ray (so the menu still works when
    // right-clicking empty sky / an empty scene).
    private Matrix GetSpawnPosition()
    {
        var result = raycastSystem.RaycastMouse();
        if (result.HasValue)
            return Utilities.TRS(result.Value.Item2, Quaternion.Identity, Vector3.One);

        var ray = engine.cameraManger.MainCamera.NormalizedScreenPointToRay(engine.inputManager.Mouse.NormalizedPosition);
        return Utilities.TRS(ray.Position + ray.Direction * 10f, Quaternion.Identity, Vector3.One);
    }

    private void SpawnLight(LightType type)
    {
        var newLight = engine.entityManager.CreateEntity(engine.entityManager.NewArchetype()
            .WithComponentData<Light>()
            .WithComponentData<LocalToWorld>());
        engine.entityManager.GetComponent<LocalToWorld>(newLight).Matrix = GetSpawnPosition();
        engine.entityManager.GetComponent<Light>(newLight) = new Light
        {
            Type = type,
            Color = new Vector4(1, 1, 1, 1),
            Intensity = type == LightType.Directional ? 1f : 1f,
            AttenuationStart = 0,
            AttenuationEnd = type == LightType.Point ? 50 : 0,
            AmbientColor = type == LightType.Directional ? new Vector4(0.2f, 0.2f, 0.2f, 1) : default,
            CastShadows = type == LightType.Directional,
        };
        engine.entityManager.GetComponent<EntityName>(newLight) =
            type == LightType.Directional ? "Directional light"u8 : "Point light"u8;
        engine.EntityInspector.InspectEntity(newLight);
    }

    private void SpawnEmptyEntity()
    {
        var newEntity = engine.entityManager.CreateEntity(
            engine.entityManager.NewArchetype().WithComponentData<LocalToWorld>(), "Entity"u8);
        engine.entityManager.GetComponent<LocalToWorld>(newEntity).Matrix = GetSpawnPosition();
        engine.EntityInspector.InspectEntity(newEntity);
    }

    // internalShaders/pbr.json is the engine's builtin metallic/roughness PBR shader (Cook-Torrance,
    // lit by the same directional/Forward+ point lights and cascaded shadows as everything else) -
    // the default material for editor placeholder primitives. Built the first time it's needed
    // rather than unconditionally at startup.
    private void EnsurePrimitiveResources()
    {
        if (primitiveMaterial != null)
            return;

        primitiveCubeMesh = engine.meshManager.CreateMesh(in CubeMesh.Instance);
        primitiveSphereMesh = engine.meshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);

        var shader = engine.shaderManager.LoadShader("internalShaders/pbr.json");
        var pipeline = engine.pipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
            new GraphicsPipelineDescription()
            {
                RasterizerState = RasterizerStateDescription.CullNone,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                BlendState = BlendStateDescription.SingleDisabled
            }, false);
        var typedMaterial = engine.materialManager.CreateMaterial<PrimitiveMaterialData_t>(pipeline);
        PrimitiveMaterialData_t data = new()
        {
            albedo = new Vector4(0.6f, 0.6f, 0.65f, 1f),
            emissive = Vector4.Zero,
            metallic = 0f,
            roughness = 0.5f,
            ao = 1f,
        };
        typedMaterial.SetMaterialData(ref data);
        primitiveMaterial = typedMaterial;

        primitiveRendererArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<PerformCullingBit>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<WorldMeshBounds>();
    }

    private void SpawnPrimitive(PrimitiveType type)
    {
        EnsurePrimitiveResources();

        var mesh = type == PrimitiveType.Cube ? primitiveCubeMesh! : primitiveSphereMesh!;
        var name = type == PrimitiveType.Cube ? "Cube"u8 : "Sphere"u8;

        var newEntity = engine.entityManager.CreateEntity(primitiveRendererArchetype!, name);
        engine.entityManager.GetComponent<LocalToWorld>(newEntity).Matrix = GetSpawnPosition();
        newEntity.SetRenderer(engine.entityManager, mesh, 0, primitiveMaterial);
        engine.EntityInspector.InspectEntity(newEntity);
    }

    public void Draw(float delta)
    {
        var display = engine.renderManager.SceneDebugReady
            ? engine.renderManager.SceneDebugTexture
            : (ITexture?)engine.renderManager.sceneViewOpaqueTexture2D;
        BeginWindow("Scene View\0"u8, display?.Handle.ToRawIntPtr() ?? IntPtr.Zero, false);

        if (ImGui.BeginPopupContextItem("##scene_context"u8))
        {
            if (ImGui.BeginMenu("Add"u8))
            {
                if (ImGui.MenuItem("Empty Entity"u8))
                    SpawnEmptyEntity();
                if (ImGui.MenuItem("Cube"u8))
                    SpawnPrimitive(PrimitiveType.Cube);
                if (ImGui.MenuItem("Sphere"u8))
                    SpawnPrimitive(PrimitiveType.Sphere);
                ImGui.Separator();
                if (ImGui.MenuItem("Point Light"u8))
                    SpawnLight(LightType.Point);
                if (ImGui.MenuItem("Directional Light"u8))
                    SpawnLight(LightType.Directional);
                ImGui.EndMenu();
            }
            ImGui.EndPopup();
        }

        if (IsVisible)
            debugViewToolbar.DrawSceneToolbar(engine);

        engine.EntityInspector.DrawSceneInspector(ViewRect);

        if (IsVisible)
            DrawViewGizmo();

        // offset past the docking tab bar (see DebugViewToolbar)
        ImGui.SetCursorPos(new Vector2(10, ImGui.GetCursorStartPos().Y + 10));
        if (ImGui.Button("sync camera"u8))
        {
            position = engine.cameraManger.MainCamera.Transform.Position;
        }
        ImGui.SetCursorPos(new Vector2(0, 0));

        if (ImGui.IsWindowFocused() || ImGui.IsWindowHovered())
        {
            UpdateCamera(delta);
        }

        EndWindow();
    }

    public void UpdateCamera(float delta)
    {
        if (engine.inputManager.mouse.RawIsMouseDown(MouseButton.Right))
        {
            yaw += engine.inputManager.Mouse.Delta.Y;
            pitch += engine.inputManager.Mouse.Delta.X;
            yaw = Math.Clamp(yaw, 0, 179);
        }

        rotation = Utilities.FromEuler(0, pitch, yaw);
        var movement = engine.inputManager.keyboard.RawGetAxis(Vectors.Down, Key.W, Key.S) +
                       engine.inputManager.keyboard.RawGetAxis(Vectors.Backward, Key.A, Key.D) +
                       engine.inputManager.keyboard.RawGetAxis(Vectors.Left, Key.E, Key.Q);

        if (movement.LengthSquared() == 0)
            currentSpeed = Math.Max(0, currentSpeed - delta * 0.01f);
        else if (currentSpeed < 1)
            currentSpeed = Math.Min(1, currentSpeed + delta * 0.001f * 0.5f);

        movement = Vectors.Normalize(movement);
        float modifier = 0.4f;
        if (engine.inputManager.keyboard.RawIsDown(Key.LeftShift))
            modifier = 15;
        if (engine.inputManager.keyboard.RawIsDown(Key.N))
            modifier = 0.1f;
        float speed = 1 * (delta / 16.0f) * modifier;
        position += movement.Multiply(rotation) * speed * currentSpeed;

        var camera = engine.cameraManger.SceneViewCamera;
        camera.Transform.Rotation = rotation;
        camera.Transform.Position = position;
    }

    // The orientation cube (ImGuizmo's ViewManipulate) in the top-right corner. Dragging it - or
    // clicking a face - rotates the scene camera. ViewManipulate edits the view matrix in place, so
    // we decompose the result back into our free-fly position/yaw/pitch (same mapping FocusOn uses)
    // or UpdateCamera would overwrite it from the stale yaw/pitch next frame.
    private void DrawViewGizmo()
    {
        const float sizePx = 100f;
        var rect = ViewRect;
        var pos = new System.Numerics.Vector2(rect.X + rect.Width - sizePx - 12, rect.Y + 36);
        var size = new System.Numerics.Vector2(sizePx, sizePx);

        var camera = engine.cameraManger.SceneViewCamera;
        Matrix view = camera.ViewMatrix;
        Matrix before = view;

        // length = orbit pivot distance in front of the camera; 0x10101010 = faint dark backdrop.
        ImGuizmo.ViewManipulate(ref view, 8f, pos, size, 0x10101010);

        if (view != before)
        {
            Matrix.Invert(view, out var world);
            position = world.Translation;
            rotation = world.Rotation();

            var eulerDeg = Utilities.ToEulerDeg(rotation); // X=Pitch, Y=Yaw, Z=Roll
            pitch = eulerDeg.Z; // roll becomes our 'pitch' arg in FromEuler(0, pitch, yaw)
            yaw = eulerDeg.X;   // pitch becomes our 'yaw' arg in FromEuler(0, pitch, yaw)

            camera.Transform.Rotation = rotation;
            camera.Transform.Position = position;
        }
    }

    // Centers and orients the SceneView camera to look at the given target position from a given distance
    public void FocusOn(in Vector3 targetPosition, float distance = 2f)
    {
        // Compute forward direction from camera to target
        var forward = Vectors.Normalize(targetPosition - position);
        if (forward.LengthSquared() < 1e-6f)
            forward = Vectors.Forward;
        // Compute rotation to look at the target
        var newRotation = Utilities.LookRotation(forward, Vectors.Up);
        rotation = newRotation;
        // Place the camera "back" from the target along the viewing direction
        position = targetPosition - forward * distance;

        // Update yaw/pitch so UpdateCamera keeps this orientation next time it runs
        var eulerDeg = Utilities.ToEulerDeg(rotation); // X=Pitch, Y=Yaw, Z=Roll
        pitch = eulerDeg.Z; // roll becomes our 'pitch' arg in FromEuler(0, pitch, yaw)
        yaw = eulerDeg.X;   // pitch becomes our 'yaw' arg in FromEuler(0, pitch, yaw)

        // Also immediately push transform to the camera
        var camera = engine.cameraManger.SceneViewCamera;
        camera.Transform.Rotation = rotation;
        camera.Transform.Position = position;
    }

    public void OnSceneViewRender()
    {
        engine.EntityInspector.SceneViewRender();
        if (gridPlane == null)
        {
            gridPlane = engine.meshManager.CreateMesh(new Vector3[]
                {
                    new Vector3(1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(1, -1, 0),
                },
                new ushort[]{0, 1, 2, 3, 4, 5});

            var shader = engine.shaderManager.LoadShader("internalShaders/grid_plane.json");

            var pipeline = engine.pipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
                new GraphicsPipelineDescription()
                {
                    RasterizerState = RasterizerStateDescription.CullNone,
                    DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                    BlendState = BlendStateDescription.Empty with
                    {
                        AttachmentStates = [BlendAttachmentDescription.AlphaBlend]
                    }
                }, false);
            material = engine.materialManager.CreateMaterial(pipeline);
        }
        engine.renderManager.Render(gridPlane, material, ShaderPassType.Forward,  0, Vector3.Zero);

        DrawGizmoIcons();
    }

    // Collects a billboard icon for each light, decal and the game camera, then draws them all in a
    // single instanced draw call (Unity-style gizmos). Only runs in the editor scene-view pass.
    private void DrawGizmoIcons()
    {
        EnsureGizmoResources();

        iconModels.Clear();
        iconDrawData.Clear();
        gizmoPickEntities.Clear();

        lightArchetype!.ForEach((IChunkDataIterator itr, int thread, int start, int end,
            ComponentDataAccess<LocalToWorld> transforms, ComponentDataAccess<Light> lights) =>
        {
            for (int i = start; i < end; i++)
            {
                if (lights[i].Disabled)
                    continue;
                AddIcon(transforms[i].Position, IconLight, LightColor, itr[i]);
            }
        });

        decalArchetype!.ForEach((IChunkDataIterator itr, int thread, int start, int end,
            ComponentDataAccess<LocalToWorld> transforms, ComponentDataAccess<Decal> decals) =>
        {
            for (int i = start; i < end; i++)
            {
                if (decals[i].Disabled)
                    continue;
                AddIcon(transforms[i].Position, IconDecal, DecalColor, itr[i]);
            }
        });

        // The camera icon tracks the game camera, which isn't a selectable entity - leave it unpickable.
        AddIcon(engine.cameraManger.MainCamera.Transform.Position, IconCamera, CameraColor, Entity.Empty);

        engine.renderManager.RenderInstanced(iconQuad!, iconMaterial!, ShaderPassType.Forward, 0,
            CollectionsMarshal.AsSpan(iconModels), CollectionsMarshal.AsSpan(iconDrawData));
    }

    private void AddIcon(Vector3 worldPosition, int iconType, int packedColor, Entity entity)
    {
        // pickId 0 = not pickable (writes "no entity"); otherwise allocate the next gizmo pick index.
        int pickId = 0;
        if (entity != Entity.Empty)
        {
            pickId = (int)(GizmoPickIdBase + (uint)gizmoPickEntities.Count);
            gizmoPickEntities.Add(entity);
        }
        iconModels.Add(Matrix.CreateTranslation(worldPosition));
        iconDrawData.Add(new Int4(iconType, packedColor, pickId, 0));
    }

    private void EnsureGizmoResources()
    {
        if (iconMaterial != null)
            return;

        iconQuad = engine.meshManager.CreateMesh(new Vector3[]
        {
            new Vector3(-1, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 1, 0),
        }, new ushort[] { 0, 1, 2, 0, 2, 3 });

        var shader = engine.shaderManager.LoadShader("internalShaders/gizmo_icon.json");
        var pipeline = engine.pipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
            new GraphicsPipelineDescription()
            {
                RasterizerState = RasterizerStateDescription.CullNone,
                // always-on-top: gizmo icons ignore scene depth so they're never hidden by geometry
                DepthStencilState = DepthStencilStateDescription.Disabled,
                BlendState = BlendStateDescription.Empty with
                {
                    AttachmentStates = [BlendAttachmentDescription.AlphaBlend]
                }
            }, false);
        iconMaterial = engine.materialManager.CreateMaterial(pipeline);

        lightArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<Light>();
        decalArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<Decal>();
    }

    private static int PackColor(float r, float g, float b, float a)
    {
        uint Ch(float v) => (uint)Math.Clamp(v * 255f + 0.5f, 0f, 255f);
        uint packed = Ch(r) | (Ch(g) << 8) | (Ch(b) << 16) | (Ch(a) << 24);
        return unchecked((int)packed);
    }
}