namespace TheEngine
{
    public interface IGame
    {
        public void Initialize(Engine engine);
        public void Update(float diff);
        public void Render(float delta);
        public event Action RequestDispose;
        
        // This method can only be executed from the engine context!
        public void DisposeGame();
    }
}