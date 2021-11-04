namespace TheEngine
{
    public interface IGame : System.IDisposable
    {
        public void Initialize(Engine engine);
        public void Update(float diff);
        public void Render(float delta);
        void SetMap(string mapPath);
    }
}