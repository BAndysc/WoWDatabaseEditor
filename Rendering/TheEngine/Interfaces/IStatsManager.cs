using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Interfaces
{
    public struct RenderStats
    {
        public int ShaderSwitches = 0;
        public int MaterialActivations = 0;
        public int MeshSwitches = 0;
        public int InstancedDraws = 0;
        public int NonInstancedDraws = 0;
        public int InstancedDrawSaved = 0;
        public int TrianglesDrawn = 0;
        public int IndicesDrawn = 0;
    }
    
    public struct PerformanceCounters
    {
        public RollingAverage BoundsCalc;
        public RollingAverage Culling;
        public RollingAverage Drawing;
        public RollingAverage TotalRender;
        public RollingAverage FrameTime;
        public RollingAverage PresentTime;
    }
    
    public interface IStatsManager
    {
        public ref PerformanceCounters Counters { get; }
        public Vector2 PixelSize { get; }
        public ref RenderStats RenderStats { get; }
    }
}