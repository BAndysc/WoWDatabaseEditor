using Avalonia.Input;
using TheEngine.Interfaces;
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
        
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        public CameraManager(ICameraManager engineCamera,
            IInputManager inputManager,
            IUIManager uiManager)
        {
            this.engineCamera = engineCamera;
            this.inputManager = inputManager;
            this.uiManager = uiManager;

            Position = new Vector3(285.396f, -4746.17f, 9.48428f + 20).ToOpenGlPosition();
            Rotation = Quaternion.LookRotation(
                new Vector3(223.698f, -4745.11f, 10.1022f + 20).ToOpenGlPosition() - Position, Vector3.Up);
            engineCamera.MainCamera.FOV = 75;
        }

        public (int, int) CurrentChunk => Position.ToWoWPosition().WoWPositionToChunk();

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

            Rotation = Quaternion.FromEuler(0, pitch, yaw);
            var movement = inputManager.Keyboard.GetAxis(Vector3.Down, Key.W, Key.S) + 
                           inputManager.Keyboard.GetAxis(Vector3.BackwardRH, Key.A, Key.D) + 
                           inputManager.Keyboard.GetAxis(Vector3.Left, Key.E, Key.Q);
            movement.Normalize();
            float speed = 1 * (delta / 16.0f) * (inputManager.Keyboard.IsDown(Key.LeftShift) ? 15 : 1);
            Position += movement * Rotation * speed;

            var camera = engineCamera.MainCamera;
            camera.Transform.Rotation = Rotation;
            camera.Transform.Position = Position;
        }

        public void RenderGUI()
        {
            using var ui = uiManager.BeginImmediateDrawRel(0, 1, 0, 1);
            ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.5f), 3);
            var wowPos = Position.ToWoWPosition();
            ui.Text("calibri", $"X: {wowPos.X:0.00} Y: {wowPos.Y:0.00} Z: {wowPos.Z:0.00}", 13, Vector4.One);
        }
    }
}