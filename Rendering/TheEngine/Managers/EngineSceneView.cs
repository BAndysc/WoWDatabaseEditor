using Avalonia.Input;
using ImGuiNET;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Entities;
using TheEngine.Interfaces;
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

    public EngineSceneView(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(float delta)
    {
        BeginWindow("Scene View\0"u8, engine.renderManager.sceneViewOpaqueTexture2D?.Handle.ToRawIntPtr() ?? IntPtr.Zero, false);

        ImGui.SetCursorPos(new Vector2(10, 10));
        if (ImGui.Button("sync camera"))
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
                        AttachmentStates = [new BlendAttachmentDescription()
                        {
                            BlendEnabled = true,
                            SourceAlphaFactor = BlendFactor.SourceAlpha,
                            DestinationAlphaFactor = BlendFactor.InverseSourceAlpha,
                        }]
                    }
                }, false);
            material = engine.materialManager.CreateMaterial(pipeline);
        }
        engine.renderManager.Render(gridPlane, material, ShaderPassType.Forward,  0, Vector3.Zero);
    }
}