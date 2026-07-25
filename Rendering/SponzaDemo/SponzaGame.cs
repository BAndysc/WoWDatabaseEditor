using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
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
        public int padding0;
        public int padding1;
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

        var mainLight = engine.LightManager.MainLight;
        mainLight.LightRotation = Utilities.LookRotation(Vectors.Normalize(new Vector3(0.3f, 0.4f, -0.85f)), Vectors.Up);
        mainLight.LightColor = new Vector4(1f, 0.97f, 0.9f, 1);
        mainLight.LightIntensity = 1f;
        mainLight.AmbientColor = new Vector4(0.38f, 0.38f, 0.42f, 1);
        engine.LightManager.SecondaryLight.LightIntensity = 0;
        engine.LightManager.Fog.Enabled = false;

        var camera = engine.CameraManager.MainCamera;
        camera.NearClip = 0.3f;
        camera.FarClip = 2000;
        camera.Transform.Position = position;

        // the engine culls by distance relative to object size (tuned for WoW maps, where
        // small props vanish at ~45 units with the default of 8); sponza's props must stay
        // visible across the whole atrium, so push the threshold beyond the scene size
        engine.RenderManager.ViewDistanceModifier = 100;

        LoadSponza();
        CreateSkybox();
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
                mat.SetTexture("texture1", diffuse);
                mat.SetTexture("maskTexture", mask ?? white);
                var data = new SponzaMaterialData_t
                {
                    diffuseColor = new Vector4(group.Material.DiffuseColor, 1),
                    alphaCutoff = mask != null ? 0.5f : -1f,
                    useMask = mask != null ? 1 : 0,
                };
                mat.SetMaterialData(ref data);
                material = materialCache[group.Material] = mat;
            }

            var mesh = engine.MeshManager.CreateMesh(in group.Mesh);
            meshes.Add(mesh);
            engine.RenderManager.RegisterStaticRenderer(mesh.Handle, material, 0, rootTransform);
        }
        Console.WriteLine($"Scene ready ({materialCache.Count} materials) in {watch.ElapsedMilliseconds} ms");
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
            material.SetTexture("texture1", texture);
            material.SetTexture("maskTexture", texture);
            var data = new SponzaMaterialData_t { diffuseColor = Vector4.One, alphaCutoff = -1 };
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
        foreach (var mesh in meshes)
            engine.MeshManager.DisposeMesh(mesh);
        meshes.Clear();
    }
}
