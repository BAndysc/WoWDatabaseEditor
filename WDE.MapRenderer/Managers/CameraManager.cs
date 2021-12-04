using Avalonia.Input;
using TheMaths;
using WDE.MapRenderer.StaticData;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapRenderer.Managers
{
    public class CameraManager
    {
        private readonly IGameContext gameContext;
        private float pitch;
        private float yaw = 26.9f;
        
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        public CameraManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            
            Position = new Vector3(285.396f, -4746.17f, 9.48428f + 20).ToOpenGlPosition();
            Rotation = Quaternion.LookRotation(
                new Vector3(223.698f, -4745.11f, 10.1022f + 20).ToOpenGlPosition() - Position, Vector3.Up);
            gameContext.Engine.CameraManager.MainCamera.FOV = 75;
        }

        public (int, int) CurrentChunk => Position.ToWoWPosition().WoWPositionToChunk();

        public void Relocate(Vector3 pos)
        {
            Position = pos;
        }
        
        public void Update(float delta)
        {
            if (gameContext.Engine.InputManager.Mouse.IsMouseDown(MouseButton.Right))
            {
                yaw -= gameContext.Engine.InputManager.Mouse.Delta.Y;
                pitch -= gameContext.Engine.InputManager.Mouse.Delta.X;
                yaw = Math.Clamp(yaw, -89, 89);
            }

            Rotation = Quaternion.FromEuler(0, pitch, yaw);
            var movement = gameContext.Engine.InputManager.Keyboard.GetAxis(Vector3.ForwardWoW, Key.W, Key.S) + 
                           gameContext.Engine.InputManager.Keyboard.GetAxis(Vector3.RightWoW, Key.A, Key.D) + 
                           gameContext.Engine.InputManager.Keyboard.GetAxis(Vector3.UpWoW, Key.E, Key.Q);
            movement.Normalize();
            float speed = 1 * (delta / 16.0f) * (gameContext.Engine.InputManager.Keyboard.IsDown(Key.LeftShift) ? 15 : 1);
            Position += movement * Rotation * speed;

            var camera = gameContext.Engine.CameraManager.MainCamera;
            camera.Transform.Rotation = Rotation;
            camera.Transform.Position = Position;
        }

        public void RenderGUI()
        {
            using var ui = gameContext.Engine.Ui.BeginImmediateDrawRel(0, 1, 0, 1);
            ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.5f), 3);
            var wowPos = Position.ToWoWPosition();
            ui.Text("calibri", $"X: {wowPos.X:0.00} Y: {wowPos.Y:0.00} Z: {wowPos.Z:0.00}", 13, Vector4.One);
        }
    }
}