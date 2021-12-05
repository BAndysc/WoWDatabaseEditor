using Avalonia.Input;
using TheEngine.Interfaces;
using TheMaths;
using IInputManager = TheEngine.Interfaces.IInputManager;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapRenderer.Managers
{
    public class ScreenSpaceSelector
    {
        private readonly IInputManager inputManager;
        private readonly IUIManager uiManager;
        private Vector2 startDragPositionNormalized;
        private Vector2 currentDragPositionNormalized;
        private Vector2 startDragPosition;
        private Vector2 currentDragPosition;
        private bool dragging;

        public event Action<Vector2, Vector2>? OnSelect;
        
        public ScreenSpaceSelector(IInputManager inputManager, IUIManager uiManager)
        {
            this.inputManager = inputManager;
            this.uiManager = uiManager;
        }

        public void Update(float diff)
        {
            bool stopDrag = dragging && !inputManager.Mouse.IsMouseDown(MouseButton.Left);
            dragging = dragging && inputManager.Mouse.IsMouseDown(MouseButton.Left);

            if (stopDrag)
            {
                var min = new Vector2(Math.Min(startDragPositionNormalized.X, currentDragPositionNormalized.X), Math.Min(startDragPositionNormalized.Y, currentDragPositionNormalized.Y));
                var max = new Vector2(Math.Max(startDragPositionNormalized.X, currentDragPositionNormalized.X), Math.Max(startDragPositionNormalized.Y, currentDragPositionNormalized.Y));
                OnSelect?.Invoke(min, max);
            }
            
            if (inputManager.Mouse.HasJustClicked(MouseButton.Left) &&
                inputManager.Keyboard.IsDown(Key.LeftShift))
            {
                startDragPosition = inputManager.Mouse.ScreenPoint;
                startDragPositionNormalized = inputManager.Mouse.NormalizedPosition;
                dragging = true;
            }

            if (dragging)
            {
                currentDragPosition = inputManager.Mouse.ScreenPoint;
                currentDragPositionNormalized = inputManager.Mouse.NormalizedPosition;
            }
        }

        public void Render()
        {
            if (dragging)
            {
                var min = new Vector2(Math.Min(startDragPosition.X, currentDragPosition.X), Math.Min(startDragPosition.Y, currentDragPosition.Y));
                var max = new Vector2(Math.Max(startDragPosition.X, currentDragPosition.X), Math.Max(startDragPosition.Y, currentDragPosition.Y));
                var size = max - min;
                
                uiManager.DrawBox(min.X, min.Y, size.X, size.Y, new Vector4(0.2f, 0.66f, 1, 0.4f));
            }
        }
    }
}