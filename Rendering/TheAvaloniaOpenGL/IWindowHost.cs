namespace TheAvaloniaOpenGL
{
    public interface IWindowHost
    {
        public float WindowWidth { get; }
        public float WindowHeight { get; }
        public float DpiScaling { get; }
        public float Aspect => (WindowWidth / WindowHeight);
    }
}