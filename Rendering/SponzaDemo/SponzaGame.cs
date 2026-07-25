using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Hexa.NET.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TheEngine.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Physics;
using TheEngine.Utils;
using TheMaths;
using Veldrid;

namespace SponzaDemo;

/// <summary>
/// A WoW-free smoke-test scene for TheEngine: the Sponza atrium (from Veldrid's NeoDemo
/// assets) with simple diffuse-textured directional lighting and a quad-based skybox.
/// No shadows, no reflections - the point is an engine-agnostic correctness scene
/// for backend work (GL today, Vulkan tomorrow).
/// </summary>a
public class SponzaGame : IGame
{
    private readonly string assetsPath;
    private Engine engine = null!;

    // free camera (same control scheme as the engine's scene view)
    private float pitch;
    private float yaw = 116.9f;
    private Vector3 position = new(-9, 0, 2.5f);
    private Quaternion rotation = Quaternion.Identity;
    private float currentSpeed;

    private readonly List<IMesh> meshes = new();
    Entity ssaoEntity;

    // physics showcase: Space spawns a falling, bouncy box at the camera; a trigger
    // volume floats above the spawn area to exercise enter/exit events separately from
    // the floor's solid contacts; left click fires a Bepu raycast along the camera's view.
    private IMesh? cubeMesh;
    private Material<SponzaMaterialData_t>? cubeMaterial;
    private IMesh? sphereMesh;
    private Material<SponzaMaterialData_t>? sphereMaterial;
    private readonly List<Entity> spawnedBodies = new();

    // parent for every entity that represents scene content (sponza meshes, point lights,
    // decals) - keeps the global/environment setup (sun, shadow config, ssao) separate at
    // the root of the entity hierarchy view.
    private Entity sceneObjectsRoot;

    // textures referenced only by their bindless index (not by a material's ITexture
    // field) have no other strong reference; keep them alive here so the GC doesn't
    // finalize and dispose them - which would invalidate the image view that the
    // bindless descriptor array still points to.
    private readonly List<ITexture> textures = new();

    // reference captures: camera pose + screenshot pairs saved under references/,
    // used to compare backend output at identical camera positions (F5 saves,
    // F6 cycles through saved poses, --replay-captures re-shoots them all)
    private static readonly string ReferencesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "references"));
    private readonly bool replayCaptures;
    private List<CameraPose> replayPoses = new();
    private int replayIndex = -1;
    private int replayFramesUntilShot;
    private int teleportIndex = -1;

    private class CameraPose
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Pitch { get; set; }
        public float Yaw { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct SponzaMaterialData_t
    {
        public Vector4 diffuseColor;
        public float alphaCutoff;
        public int useMask;
        public int texture1Index;
        public int maskTextureIndex;
    }

    public SponzaGame(string assetsPath, bool replayCaptures = false)
    {
        this.assetsPath = assetsPath;
        this.replayCaptures = replayCaptures;
    }

    public event Action? RequestDispose;

    public bool Initialize(Engine engine)
    {
        this.engine = engine;

        // light direction is the entity forward; YawPitchRoll order matches the inspector's TRS editor.
        var sunEuler = new Vector3(340f, 82f, 22f) * MathUtil.Deg2Rad;
        var sun = engine.EntityManager.CreateEntity(engine.EntityManager.NewArchetype()
            .WithComponentData<Light>()
            .WithComponentData<LocalToWorld>(), "Sun"u8);
        engine.EntityManager.GetComponent<LocalToWorld>(sun).Matrix =
            Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(sunEuler.Y, sunEuler.X, sunEuler.Z));
        engine.EntityManager.GetComponent<Light>(sun) = new Light
        {
            Type = LightType.Directional,
            Color = new Vector4(1f, 0.95f, 0.82f, 1),
            Intensity = 2.4f,
            AmbientColor = new Vector4(0.11f, 0.12f, 0.18f, 1),
            CastShadows = true,
        };

        // shadows render only while a CascadeShadowMap entity exists (see CascadeShadowMap).
        var shadowConfig = engine.EntityManager.CreateEntity(engine.EntityManager.NewArchetype()
            .WithComponentData<CascadeShadowMap>(), "Shadow Config"u8);
        engine.EntityManager.GetComponent<CascadeShadowMap>(shadowConfig) = CascadeShadowMap.CreateDefault();
        engine.CameraManager.MainCamera.Fog = new FogSettings() { Enabled = false };

        var camera = engine.CameraManager.MainCamera;
        camera.NearClip = 0.3f;
        camera.FarClip = 2000;
        camera.Transform.Position = position;

        // the engine culls by distance relative to object size (tuned for WoW maps, where
        // small props vanish at ~45 units with the default of 8); sponza's props must stay
        // visible across the whole atrium, so push the threshold beyond the scene size
        engine.CameraManager.MainCamera.ViewDistanceModifier = 100;

        sceneObjectsRoot = engine.EntityManager.CreateEntity(engine.EntityManager.NewArchetype(), "Scene Objects"u8);

        LoadSponza();
        CreateSkybox();
        CreatePointLights();
        CreateDecals();
        CreatePhysicsDemo();

        ssaoEntity = engine.EntityManager.CreateEntity(engine.entityManager.NewArchetype());
        engine.entityManager.GetComponent<EntityName>(ssaoEntity) = "Ambient Occlussion"u8;
        engine.EntityManager.AddComponent(ssaoEntity, new AmbientOcclusion());

        return true;
    }

    private void LoadSponza()
    {
        var objPath = Path.Combine(assetsPath, "Models", "SponzaAtrium", "sponza.obj");
        Console.WriteLine($"Loading {objPath}...");
        var watch = Stopwatch.StartNew();
        var groups = WavefrontLoader.Load(objPath);
        Console.WriteLine($"Parsed {groups.Count} groups in {watch.ElapsedMilliseconds} ms");

        var shader = engine.ShaderManager.LoadShader("data/sponza_lit.json");
        // cull-none keeps the demo independent from the exporter's winding; sponza is cheap
        var pipeline = engine.PipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
            new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleDisabled,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = RasterizerStateDescription.CullNone,
            }, false);

        var white = engine.TextureManager.CreateTexture(new uint[] { 0xFFFFFFFF }, 1, 1);
        textures.Add(white);

        // sponza is Y-up; the engine is Z-up. NeoDemo used the same 0.1 scale.
        var rootTransform = Matrix.CreateScale(0.1f) * Matrix.CreateRotationX(MathF.PI / 2);

        var materialCache = new Dictionary<WavefrontLoader.ObjMaterial, Material>();
        foreach (var group in groups)
        {
            if (!materialCache.TryGetValue(group.Material, out var material))
            {
                var mat = engine.MaterialManager.CreateMaterial<SponzaMaterialData_t>(pipeline);
                var diffuse = group.Material.DiffuseTexture != null && File.Exists(group.Material.DiffuseTexture)
                    ? engine.TextureManager.LoadTexture(group.Material.DiffuseTexture)
                    : white;
                var mask = group.Material.MaskTexture != null && File.Exists(group.Material.MaskTexture)
                    ? engine.TextureManager.LoadTexture(group.Material.MaskTexture)
                    : null;
                var data = new SponzaMaterialData_t
                {
                    diffuseColor = new Vector4(group.Material.DiffuseColor, 1),
                    alphaCutoff = mask != null ? 0.5f : -1f,
                    useMask = mask != null ? 1 : 0,
                    texture1Index = engine.TextureManager.GetBindlessIndex(diffuse),
                    maskTextureIndex = engine.TextureManager.GetBindlessIndex(mask ?? white),
                };
                mat.SetMaterialData(ref data);
                material = materialCache[group.Material] = mat;
            }

            var mesh = engine.MeshManager.CreateMesh(in group.Mesh);
            meshes.Add(mesh);
            var handle = engine.RenderManager.RegisterStaticRenderer(mesh.Handle, material, 0, rootTransform);
            engine.EntityManager.AddComponent<LegacyCollider>(handle.Handle, new LegacyCollider());
            engine.EntityManager.GetComponent<EntityName>(handle.Handle) = group.Material.Name;
            engine.EntityManager.SetParent(handle.Handle, sceneObjectsRoot);
        }
        Console.WriteLine($"Scene ready ({materialCache.Count} materials) in {watch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// A handful of Forward+ point lights scattered around the atrium, for interactive
    /// verification of tiled light culling (see internalShaders/light_cull.comp).
    /// </summary>
    private void CreatePointLights()
    {
        var archetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<Light>();

        // positions/ranges are tuned to the atrium's scale (the camera roams roughly
        // X in [-130, 110], Y in [-40, 47], Z in [0, 80] - see references/capture_*.json)
        (Vector3 pos, Vector3 color, float intensity, float range)[] lights =
        {
            (new(-60, 0, 15), new(1f, 0.4f, 0.2f), 4f, 50f),
            (new(-20, 20, 15), new(0.2f, 0.6f, 1f), 4f, 50f),
            (new(40, -10, 20), new(0.3f, 1f, 0.3f), 4f, 50f),
            (new(0, 0, 30), new(1f, 0.2f, 0.8f), 4f, 60f),
        };

        foreach (var (pos, color, intensity, range) in lights)
        {
            var entity = engine.entityManager.CreateEntity(archetype);
            entity.SetTRS(engine.entityManager, pos, Quaternion.Identity, Vector3.One);
            engine.entityManager.GetComponent<Light>(entity) = new Light
            {
                Type = LightType.Point,
                AttenuationStart = range * 0.3f,
                AttenuationEnd = range,
                Intensity = intensity,
                Color = new Vector4(color, 1),
            };
            engine.entityManager.SetParent(entity, sceneObjectsRoot);
        }
    }

    /// <summary>
    /// A single Forward+ decal on the atrium floor, for interactive verification of tiled
    /// decal culling (see internalShaders/tile_cull.comp) and ApplyDecals() in theengine.cginc.
    /// Uses a procedurally-generated soft circular splat so the demo needs no extra texture asset.
    /// </summary>
    private void CreateDecals()
    {
        const int size = 64;
        var pixels = new Vector4[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float falloff = 1f - Math.Clamp(MathF.Sqrt(u * u + v * v), 0f, 1f);
                pixels[y * size + x] = new Vector4(1f, 1f, 1f, falloff * falloff);
            }
        }
        var splatTexture = engine.TextureManager.CreateTexture(pixels, size, size);

        var archetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<Decal>();

        // identity rotation -> the decal's local Z axis (its projection axis) is world up
        // (Vectors.Up == UnitZ), so it projects straight down onto the atrium floor below it.
        var entity = engine.entityManager.CreateEntity(archetype);
        entity.SetTRS(engine.entityManager, new Vector3(0, 0, 0.5f), Quaternion.Identity, new Vector3(10, 10, 0.5f));
        engine.entityManager.GetComponent<Decal>(entity) = new Decal
        {
            Albedo = splatTexture,
            Color = new Vector4(1f, 0.2f, 0.2f, 0.8f),
            FadeAngleCos = 0.5f,
        };
        engine.entityManager.SetParent(entity, sceneObjectsRoot);
    }

    /// <summary>
    /// Static floor + a floating trigger volume (proves Bepu statics/triggers), plus the
    /// cube mesh/material reused by every dynamic box <see cref="Key.Space"/> spawns.
    /// </summary>
    private void CreatePhysicsDemo()
    {
        engine.PhysicsManager.ContactBegin += (a, b) => Console.WriteLine($"[Physics] Contact begin: {a} <-> {b}");
        engine.PhysicsManager.ContactEnd += (a, b) => Console.WriteLine($"[Physics] Contact end: {a} <-> {b}");

        var floorArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<BoxCollider>();
        var floor = engine.entityManager.CreateEntity(floorArchetype, "Physics Floor"u8);
        floor.SetTRS(engine.entityManager, new Vector3(0, 0, -0.1f), Quaternion.Identity, Vector3.One);
        engine.entityManager.GetComponent<BoxCollider>(floor) = new BoxCollider { Size = new Vector3(300, 150, 0.2f) };
        engine.entityManager.SetParent(floor, sceneObjectsRoot);

        var triggerArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<BoxCollider>()
            .WithComponentData<Trigger>();
        var trigger = engine.entityManager.CreateEntity(triggerArchetype, "Physics Trigger"u8);
        trigger.SetTRS(engine.entityManager, new Vector3(0, 0, 5), Quaternion.Identity, Vector3.One);
        engine.entityManager.GetComponent<BoxCollider>(trigger) = new BoxCollider { Size = new Vector3(10, 10, 1) };
        engine.entityManager.SetParent(trigger, sceneObjectsRoot);

        var shader = engine.ShaderManager.LoadShader("data/sponza_lit.json");
        var pipeline = engine.PipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
            new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleDisabled,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = RasterizerStateDescription.CullNone,
            }, false);
        var white = engine.TextureManager.CreateTexture(new uint[] { 0xFFFFFFFF }, 1, 1);
        textures.Add(white);
        var whiteIndex = engine.TextureManager.GetBindlessIndex(white);

        cubeMaterial = engine.MaterialManager.CreateMaterial<SponzaMaterialData_t>(pipeline);
        var data = new SponzaMaterialData_t { diffuseColor = new Vector4(1f, 0.35f, 0.3f, 1), alphaCutoff = -1, texture1Index = whiteIndex, maskTextureIndex = whiteIndex };
        cubeMaterial.SetMaterialData(ref data);
        cubeMesh = engine.MeshManager.CreateMesh(CreateUnitCubeMeshData());
        meshes.Add(cubeMesh);

        CreateBounceSpheres(pipeline, whiteIndex);
    }

    /// <summary>
    /// A handful of pre-placed dynamic, highly-bouncy spheres dropped into the atrium at
    /// scene load, so the physics showcase is visible immediately without pressing Space.
    /// </summary>
    private void CreateBounceSpheres(TheEngine.Resources.Pipeline pipeline, int whiteIndex)
    {
        sphereMaterial = engine.MaterialManager.CreateMaterial<SponzaMaterialData_t>(pipeline);
        var data = new SponzaMaterialData_t { diffuseColor = new Vector4(0.25f, 0.55f, 1f, 1), alphaCutoff = -1, texture1Index = whiteIndex, maskTextureIndex = whiteIndex };
        sphereMaterial.SetMaterialData(ref data);
        sphereMesh = engine.MeshManager.CreateMesh(CreateUnitSphereMeshData());
        meshes.Add(sphereMesh);

        // dropped from well above the floor, scattered across the atrium's roamable area
        // (X in [-130, 110], Y in [-40, 47] - see CreatePointLights)
        (Vector3 pos, float radius)[] spheres =
        {
            (new(-60, 10, 25), 1.2f),
            (new(-20, -15, 30), 0.8f),
            (new(20, 20, 35), 1.5f),
            (new(50, -5, 20), 1.0f),
            (new(-80, 25, 28), 0.9f),
        };

        foreach (var (pos, radius) in spheres)
        {
            var handle = engine.RenderManager.RegisterDynamicRenderer(sphereMesh.Handle, sphereMaterial, 0, Matrix.Identity);
            var entity = handle.Handle;
            entity.SetTRS(engine.entityManager, pos, Quaternion.Identity, new Vector3(radius * 2));
            engine.entityManager.AddComponent(entity, new SphereCollider { Radius = radius });
            engine.entityManager.AddComponent(entity, new RigidBody { Type = BodyType.Dynamic, Mass = 4f / 3f * MathF.PI * radius * radius * radius });
            engine.entityManager.AddComponent(entity, new PhysicsMaterial { Friction = 0.3f, Bounciness = 0.85f });
            engine.entityManager.SetParent(entity, sceneObjectsRoot);
            spawnedBodies.Add(entity);
        }
    }

    private static MeshData CreateUnitSphereMeshData(int rings = 12, int segments = 16)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<ushort>();

        // pole axis is world-up (Z) purely by convention - the sphere is rotationally
        // symmetric so it doesn't affect shading or collider alignment.
        for (int r = 0; r <= rings; ++r)
        {
            float v = (float)r / rings;
            float phi = v * MathF.PI;
            float ringRadius = MathF.Sin(phi);
            float z = MathF.Cos(phi);
            for (int s = 0; s <= segments; ++s)
            {
                float u = (float)s / segments;
                float theta = u * MathF.PI * 2;
                var normal = new Vector3(ringRadius * MathF.Cos(theta), ringRadius * MathF.Sin(theta), z);
                positions.Add(normal * 0.5f);
                normals.Add(normal);
                uvs.Add(new Vector2(u, v));
            }
        }

        int vertsPerRing = segments + 1;
        for (int r = 0; r < rings; ++r)
        {
            for (int s = 0; s < segments; ++s)
            {
                int i0 = r * vertsPerRing + s;
                int i1 = i0 + 1;
                int i2 = i0 + vertsPerRing;
                int i3 = i2 + 1;
                indices.Add((ushort)i0); indices.Add((ushort)i1); indices.Add((ushort)i2);
                indices.Add((ushort)i2); indices.Add((ushort)i1); indices.Add((ushort)i3);
            }
        }

        return new MeshData(positions.ToArray(), normals.ToArray(), uvs.ToArray(), indices.ToArray());
    }

    private static MeshData CreateUnitCubeMeshData()
    {
        (Vector3 normal, Vector3 right, Vector3 up)[] faces =
        {
            (Vectors.Forward, Vectors.Left, Vectors.Up),
            (Vectors.Backward, Vectors.Right, Vectors.Up),
            (Vectors.Left, Vectors.Backward, Vectors.Up),
            (Vectors.Right, Vectors.Forward, Vectors.Up),
            (Vectors.Up, Vectors.Forward, Vectors.Left),
            (Vectors.Down, Vectors.Forward, Vectors.Right),
        };

        var positions = new Vector3[24];
        var normals = new Vector3[24];
        var uvs = new Vector2[24];
        var indices = new ushort[36];

        for (int f = 0; f < 6; ++f)
        {
            var (normal, right, up) = faces[f];
            var center = normal * 0.5f;
            positions[f * 4 + 0] = center - right * 0.5f - up * 0.5f;
            positions[f * 4 + 1] = center + right * 0.5f - up * 0.5f;
            positions[f * 4 + 2] = center - right * 0.5f + up * 0.5f;
            positions[f * 4 + 3] = center + right * 0.5f + up * 0.5f;
            for (int i = 0; i < 4; ++i)
                normals[f * 4 + i] = normal;
            uvs[f * 4 + 0] = new Vector2(0, 0);
            uvs[f * 4 + 1] = new Vector2(1, 0);
            uvs[f * 4 + 2] = new Vector2(0, 1);
            uvs[f * 4 + 3] = new Vector2(1, 1);
            indices[f * 6 + 0] = (ushort)(f * 4 + 0);
            indices[f * 6 + 1] = (ushort)(f * 4 + 1);
            indices[f * 6 + 2] = (ushort)(f * 4 + 2);
            indices[f * 6 + 3] = (ushort)(f * 4 + 2);
            indices[f * 6 + 4] = (ushort)(f * 4 + 1);
            indices[f * 6 + 5] = (ushort)(f * 4 + 3);
        }

        return new MeshData(positions, normals, uvs, indices);
    }

    private void SpawnPhysicsBox()
    {
        var forward = Vectors.Down.Multiply(rotation);
        var spawnPos = position + forward * 3f;
        const float size = 0.6f;

        var handle = engine.RenderManager.RegisterDynamicRenderer(cubeMesh!.Handle, cubeMaterial!, 0, Matrix.Identity);
        var entity = handle.Handle;
        entity.SetTRS(engine.entityManager, spawnPos, Quaternion.Identity, new Vector3(size));
        engine.entityManager.AddComponent(entity, new BoxCollider { Size = new Vector3(size, size, size) });
        engine.entityManager.AddComponent(entity, new RigidBody { Type = BodyType.Dynamic, Mass = 2 });
        engine.entityManager.AddComponent(entity, new PhysicsMaterial { Friction = 0.6f, Bounciness = 0.4f });
        spawnedBodies.Add(entity);
    }

    private void CreateSkybox()
    {
        var shader = engine.ShaderManager.LoadShader("data/sponza_sky.json");
        var pipeline = engine.PipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
            new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleDisabled,
                // LessEqual lets the sky's depth==1.0 pass against the cleared depth buffer
                DepthStencilState = new DepthStencilStateDescription(true, false, ComparisonKind.LessEqual),
                RasterizerState = RasterizerStateDescription.CullNone,
            }, false);

        const float s = 500;
        // each face: center, right axis, up axis (Z-up world; +Y is "north")
        (string suffix, Vector3 center, Vector3 right, Vector3 up)[] faces =
        {
            ("ft", new(0, s, 0), new(-1, 0, 0), new(0, 0, 1)),
            ("bk", new(0, -s, 0), new(1, 0, 0), new(0, 0, 1)),
            ("lf", new(-s, 0, 0), new(0, -1, 0), new(0, 0, 1)),
            ("rt", new(s, 0, 0), new(0, 1, 0), new(0, 0, 1)),
            ("up", new(0, 0, s), new(-1, 0, 0), new(0, -1, 0)),
            ("dn", new(0, 0, -s), new(-1, 0, 0), new(0, 1, 0)),
        };

        foreach (var face in faces)
        {
            var texturePath = Path.Combine(assetsPath, "Textures", "cloudtop", $"cloudtop_{face.suffix}.png");
            if (!File.Exists(texturePath))
                continue;
            var texture = engine.TextureManager.LoadTexture(texturePath);
            engine.TextureManager.SetWrapping(texture, WrapMode.ClampToEdge);

            var material = engine.MaterialManager.CreateMaterial<SponzaMaterialData_t>(pipeline);
            var textureIndex = engine.TextureManager.GetBindlessIndex(texture);
            var data = new SponzaMaterialData_t { diffuseColor = Vector4.One, alphaCutoff = -1, texture1Index = textureIndex, maskTextureIndex = textureIndex };
            material.SetMaterialData(ref data);

            var mesh = engine.MeshManager.CreateMesh(new MeshData(
                new[]
                {
                    face.center - face.right * s + face.up * s,
                    face.center + face.right * s + face.up * s,
                    face.center - face.right * s - face.up * s,
                    face.center + face.right * s - face.up * s,
                },
                new[] { Vectors.Up, Vectors.Up, Vectors.Up, Vectors.Up },
                new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) },
                new ushort[] { 0, 1, 2, 2, 1, 3 }));
            meshes.Add(mesh);
            engine.RenderManager.RegisterStaticRenderer(mesh.Handle, material, 0, Matrix.Identity);
        }
    }

    public void Update(float delta)
    {
        if (replayCaptures)
        {
            UpdateReplay();
            return;
        }

        var input = engine.InputManager;

        if (input.Keyboard.JustPressed(Key.F5))
            SaveReferenceCapture();
        if (input.Keyboard.JustPressed(Key.F6))
            TeleportToNextSavedPose();
        if (input.Keyboard.JustPressed(Key.Space))
            SpawnPhysicsBox();
        if (input.Mouse.HasJustClicked(TheEngine.Input.MouseButton.Left))
        {
            var rayDirection = Vectors.Down.Multiply(rotation);
            if (engine.PhysicsManager.RayCast(new Ray(position, rayDirection), 1000f, out var hit))
                Console.WriteLine($"[Physics] Raycast hit {hit.Entity} at {hit.Point} (distance {hit.Distance:0.00})");
            else
                Console.WriteLine("[Physics] Raycast hit nothing");
        }

        if (input.Mouse.IsMouseDown(TheEngine.Input.MouseButton.Right))
        {
            yaw += input.Mouse.Delta.Y;
            pitch += input.Mouse.Delta.X;
            yaw = Math.Clamp(yaw, 0, 179);
        }

        rotation = Utilities.FromEuler(0, pitch, yaw);
        var movement = input.Keyboard.GetAxis(Vectors.Down, Key.W, Key.S) +
                       input.Keyboard.GetAxis(Vectors.Backward, Key.A, Key.D) +
                       input.Keyboard.GetAxis(Vectors.Left, Key.E, Key.Q);

        if (movement.LengthSquared() == 0)
            currentSpeed = Math.Max(0, currentSpeed - delta * 0.01f);
        else if (currentSpeed < 1)
            currentSpeed = Math.Min(1, currentSpeed + delta * 0.001f * 0.5f);

        movement = Vectors.Normalize(movement);
        float modifier = input.Keyboard.IsDown(Key.LeftShift) ? 4f : 0.8f;
        float speed = delta / 16.0f * modifier;
        position += movement.Multiply(rotation) * speed * currentSpeed;

        var camera = engine.CameraManager.MainCamera;
        camera.Transform.Rotation = rotation;
        camera.Transform.Position = position;

        engine.EntityManager.GetComponent<AmbientOcclusion>(ssaoEntity).Enabled = false;
    }

    private void ApplyPose(CameraPose pose)
    {
        position = new Vector3(pose.X, pose.Y, pose.Z);
        pitch = pose.Pitch;
        yaw = pose.Yaw;
        rotation = Utilities.FromEuler(0, pitch, yaw);
        var camera = engine.CameraManager.MainCamera;
        camera.Transform.Rotation = rotation;
        camera.Transform.Position = position;
    }

    private void Screenshot(string path)
    {
        // reads the previously completed frame - with a static camera that is exactly
        // the saved pose. The backend writes a correctly oriented PNG.
        var backBuffer = ((RenderManager)engine.RenderManager).CurrentBackBuffer;
        engine.TextureManager.ScreenshotRenderTexture(backBuffer, path);
    }

    private void SaveReferenceCapture()
    {
        Directory.CreateDirectory(ReferencesPath);
        int index = 0;
        while (File.Exists(Path.Combine(ReferencesPath, $"capture_{index:000}.json")))
            index++;
        var baseName = Path.Combine(ReferencesPath, $"capture_{index:000}");
        var pose = new CameraPose { X = position.X, Y = position.Y, Z = position.Z, Pitch = pitch, Yaw = yaw };
        File.WriteAllText(baseName + ".json", JsonSerializer.Serialize(pose, new JsonSerializerOptions { WriteIndented = true }));
        Screenshot(baseName + ".png");
        Console.WriteLine($"Saved reference capture: {baseName}.png");
    }

    private static List<CameraPose> LoadSavedPoses()
        => Directory.Exists(ReferencesPath)
            ? Directory.GetFiles(ReferencesPath, "capture_*.json")
                .Order()
                .Select(f => JsonSerializer.Deserialize<CameraPose>(File.ReadAllText(f))!)
                .ToList()
            : new List<CameraPose>();

    private void TeleportToNextSavedPose()
    {
        var poses = LoadSavedPoses();
        if (poses.Count == 0)
        {
            Console.WriteLine("No saved captures to teleport to (press F5 to create one)");
            return;
        }
        teleportIndex = (teleportIndex + 1) % poses.Count;
        ApplyPose(poses[teleportIndex]);
        Console.WriteLine($"Teleported to capture {teleportIndex}");
    }

    // --replay-captures: visit every saved pose, let the frame settle, re-shoot it as
    // replay_NNN.png and exit - re-runnable on any backend to diff against capture_NNN.png
    private void UpdateReplay()
    {
        // the engine boots with the Scene View tab focused; the captures are of the game
        // view ("3D" tab), which only renders while its tab is visible
        ImGui.SetWindowFocus("3D");

        if (replayIndex == -1)
        {
            replayPoses = LoadSavedPoses();
            if (replayPoses.Count == 0)
            {
                Console.WriteLine("No saved captures to replay");
                Environment.Exit(1);
            }
            replayIndex = 0;
            replayFramesUntilShot = 90;
            ApplyPose(replayPoses[0]);
            return;
        }

        if (--replayFramesUntilShot > 0)
            return;

        var path = Path.Combine(ReferencesPath, $"replay_{replayIndex:000}.png");
        Screenshot(path);
        Console.WriteLine($"Replayed capture {replayIndex}: {path}");

        replayIndex++;
        if (replayIndex >= replayPoses.Count)
        {
            Console.WriteLine("All captures replayed");
            Environment.Exit(0);
        }
        replayFramesUntilShot = 90;
        ApplyPose(replayPoses[replayIndex]);
    }

    public void Render(float delta)
    {
    }

    public void RenderTransparent(float delta)
    {
    }

    public void RenderGUI(float delta)
    {
    }

    public void DisposeGame()
    {
        engine.EntityManager.DestroyEntity(ssaoEntity);
        foreach (var entity in spawnedBodies)
            engine.EntityManager.DestroyEntity(entity);
        spawnedBodies.Clear();
        foreach (var mesh in meshes)
            engine.MeshManager.DisposeMesh(mesh);
        meshes.Clear();
        foreach (var texture in textures)
            engine.TextureManager.DisposeTexture(texture);
        textures.Clear();
    }
}
