using Avalonia.Input;
using ImGuiNET;
using TheEngine.Interfaces;
using TheEngine.Utils.ImGuiHelper;
using TheMaths;
using WDE.MapRenderer.StaticData;
using IInputManager = TheEngine.Interfaces.IInputManager;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapRenderer.Managers
{
    public class CameraManager
    {
        private readonly ICameraManager engineCamera;
        private readonly IInputManager inputManager;
        private readonly IUIManager uiManager;
        private float pitch;
        private float yaw = 26.9f + 90;

        private SimpleBox coordNotificationBox;

        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        private float currentSpeed = 0;

        public CameraManager(ICameraManager engineCamera,
            IInputManager inputManager,
            IUIManager uiManager)
        {
            this.engineCamera = engineCamera;
            this.inputManager = inputManager;
            this.uiManager = uiManager;

            Position = new Vector3(285.396f, -4746.17f, 9.48428f + 20);
            Rotation = Utilities.LookRotation(
                new Vector3(223.698f, -4745.11f, 10.1022f + 20) - Position, Vectors.Up);
            
            engineCamera.MainCamera.FOV = 75;
            this.coordNotificationBox = new SimpleBox(BoxPlacement.BottomLeft);
        }

        public (int, int) CurrentChunk => Position.WoWPositionToChunk();

        public void Relocate(Vector3 pos)
        {
            Position = pos;
        }
        
        public void Update(float delta)
        {
            if (inputManager.Mouse.IsMouseDown(MouseButton.Right))
            {
                yaw += inputManager.Mouse.Delta.Y;
                pitch += inputManager.Mouse.Delta.X;
                yaw = Math.Clamp(yaw, 0, 179);
            }
            
            Rotation = Utilities.FromEuler(0, pitch, yaw);
            var movement = inputManager.Keyboard.GetAxis(Vectors.Down, Key.W, Key.S) + 
                           inputManager.Keyboard.GetAxis(Vectors.Backward, Key.A, Key.D) + 
                           inputManager.Keyboard.GetAxis(Vectors.Left, Key.E, Key.Q);

            if (movement.LengthSquared() == 0)
                currentSpeed = Math.Max(0, currentSpeed - delta * 0.01f);
            else if (currentSpeed < 1)
                currentSpeed = Math.Min(1, currentSpeed + delta * 0.001f * 0.5f);
            
            movement = Vectors.Normalize(movement);
            float modifier = 0.4f;
            if (inputManager.Keyboard.IsDown(Key.LeftShift))
                modifier = 15;
            if (inputManager.Keyboard.IsDown(Key.N))
                modifier = 0.1f;
            float speed = 1 * (delta / 16.0f) * modifier;
            Position += movement.Multiply(Rotation) * speed * currentSpeed;

            var camera = engineCamera.MainCamera;
            camera.Transform.Rotation = Rotation;
            camera.Transform.Position = Position;
        }

        public void RenderGUI()
        {
            coordNotificationBox.Draw($"X: {Position.X:0.00} Y: {Position.Y:0.00} Z: {Position.Z:0.00}");
        }
    }
}
