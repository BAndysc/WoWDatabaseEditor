using Avalonia.Input;
using TheMaths;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapRenderer.Managers
{
    public class ScreenSpaceSelector
    {
        private readonly IGameContext gameContext;

        private Vector2 startDragPositionNormalized;
        private Vector2 currentDragPositionNormalized;
        private Vector2 startDragPosition;
        private Vector2 currentDragPosition;
        private bool dragging;

        public event Action<Vector2, Vector2>? OnSelect;
        
        public ScreenSpaceSelector(IGameContext gameContext)
        {
            this.gameContext = gameContext;
        }

        public void Update(float diff)
        {
            bool stopDrag = dragging && !gameContext.Engine.InputManager.Mouse.IsMouseDown(MouseButton.Left);
            dragging = dragging && gameContext.Engine.InputManager.Mouse.IsMouseDown(MouseButton.Left);

            if (stopDrag)
            {
                var min = new Vector2(Math.Min(startDragPositionNormalized.X, currentDragPositionNormalized.X), Math.Min(startDragPositionNormalized.Y, currentDragPositionNormalized.Y));
                var max = new Vector2(Math.Max(startDragPositionNormalized.X, currentDragPositionNormalized.X), Math.Max(startDragPositionNormalized.Y, currentDragPositionNormalized.Y));
                OnSelect?.Invoke(min, max);
            }
            
            if (gameContext.Engine.InputManager.Mouse.HasJustClicked(MouseButton.Left) &&
                gameContext.Engine.InputManager.Keyboard.IsDown(Key.LeftShift))
            {
                startDragPosition = gameContext.Engine.InputManager.Mouse.ScreenPoint;
                startDragPositionNormalized = gameContext.Engine.InputManager.Mouse.NormalizedPosition;
                dragging = true;
            }

            if (dragging)
            {
                currentDragPosition = gameContext.Engine.InputManager.Mouse.ScreenPoint;
                currentDragPositionNormalized = gameContext.Engine.InputManager.Mouse.NormalizedPosition;
            }
        }

        public void Render()
        {
            if (dragging)
            {
                var min = new Vector2(Math.Min(startDragPosition.X, currentDragPosition.X), Math.Min(startDragPosition.Y, currentDragPosition.Y));
                var max = new Vector2(Math.Max(startDragPosition.X, currentDragPosition.X), Math.Max(startDragPosition.Y, currentDragPosition.Y));
                var size = max - min;
                
                gameContext.Engine.Ui.DrawBox(min.X, min.Y, size.X, size.Y, new Vector4(0.2f, 0.66f, 1, 0.4f));
            }
        }
    }
}