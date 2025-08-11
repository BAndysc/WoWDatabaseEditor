using Avalonia.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheEngineTest;
using TheMaths;
using Veldrid;
using MouseButton = TheEngine.Input.MouseButton;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

var nativeWindowSettings = new NativeWindowSettings()
{
    Size = new Vector2i(1280, 720),
    Title = "WoW Database Editor - 3D Debug view",
    // This is needed to run on macos
    Flags = ContextFlags.ForwardCompatible,
};

var context = new SingleThreadSynchronizationContext(Environment.CurrentManagedThreadId);

SynchronizationContext.SetSynchronizationContext(context);

var game = new MyGame();

using var window = new GameStandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, game, context);
window.Run();
TheEngine.TheEngine.Deinit();

namespace TheEngineTest
{
    struct UnlitMaterial
    {
        public Vector4 color;
    }

    class MyGame : IGame
    {
        private IMesh sphereMesh;
        private Engine engine;
        private Material<UnlitMaterial> material;

        public bool Initialize(Engine engine)
        {
            this.engine = engine;
            sphereMesh = engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);

            var shader = engine.ShaderManager.LoadShader("internalShaders/unlit.json");

            var pipeline = engine.PipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList,
                new GraphicsPipelineDescription()
                {
                    BlendState = new BlendStateDescription()
                    {
                        AttachmentStates = [new BlendAttachmentDescription()
                        {
                            BlendEnabled = false
                        }]
                    },
                    RasterizerState = RasterizerStateDescription.CullNone,
                    DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                }, false);

            material = engine.MaterialManager.CreateMaterial<UnlitMaterial>(pipeline);
            UnlitMaterial data = new() { color = new Vector4(1, 0, 0, 1) };
            material.SetMaterialData(ref data);

            var entity =
                this.engine.RenderManager.RegisterDynamicRenderer(sphereMesh.Handle, material, 0, Matrix4x4.Identity);
            return true;
        }

        public void Update(float diff)
        {
            UpdateCamera(diff);
        }

        private float pitch;
        private float yaw = 26.9f + 90;
        public Vector3 position;
        private Quaternion rotation;
        private float currentSpeed = 0;

        public void UpdateCamera(float delta)
        {
            if (engine.inputManager.mouse.IsMouseDown(MouseButton.Right))
            {
                yaw += engine.inputManager.Mouse.Delta.Y;
                pitch += engine.inputManager.Mouse.Delta.X;
                yaw = Math.Clamp(yaw, 0, 179);
            }

            rotation = Utilities.FromEuler(0, pitch, yaw);
            var movement = engine.inputManager.keyboard.GetAxis(Vectors.Down, Key.W, Key.S) +
                           engine.inputManager.keyboard.GetAxis(Vectors.Backward, Key.A, Key.D) +
                           engine.inputManager.keyboard.GetAxis(Vectors.Left, Key.E, Key.Q);

            if (movement.LengthSquared() == 0)
                currentSpeed = Math.Max(0, currentSpeed - delta * 0.01f);
            else if (currentSpeed < 1)
                currentSpeed = Math.Min(1, currentSpeed + delta * 0.001f * 0.5f);

            movement = Vectors.Normalize(movement);
            float modifier = 0.4f;
            if (engine.inputManager.keyboard.IsDown(Key.LeftShift))
                modifier = 15;
            if (engine.inputManager.keyboard.IsDown(Key.N))
                modifier = 0.1f;
            float speed = 1 * (delta / 16.0f) * modifier;
            position += movement.Multiply(rotation) * speed * currentSpeed;

            var camera = engine.cameraManger.MainCamera;
            camera.Transform.Rotation = rotation;
            camera.Transform.Position = position;
        }

        public void Render(float delta)
        {
            // engine.RenderManager.Render(sphereMesh, material, ShaderPassType.Forward, 0, Matrix4x4.Identity);
        }

        public void RenderTransparent(float delta)
        {
        }

        public void RenderGUI(float delta)
        {
        }

        public event Action? RequestDispose;
        public void DisposeGame()
        {
        }
    }
}