namespace TheEngine
{
    public interface IGame
    {
        public bool Initialize(Engine engine);
        public void Update(float diff);
        public void Render(float delta);
        public void RenderTransparent(float delta);
        public void RenderGUI(float delta);
        public event Action RequestDispose;
        
        // This method can only be executed from the engine context!
        public void DisposeGame();
    }
}