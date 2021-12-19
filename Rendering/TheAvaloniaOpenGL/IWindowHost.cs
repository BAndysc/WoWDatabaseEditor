namespace TheAvaloniaOpenGL
{
    public interface IWindowHost
    {
        public float WindowWidth { get; }
        public float WindowHeight { get; }
        public float Aspect => (WindowWidth / WindowHeight);
    }
}