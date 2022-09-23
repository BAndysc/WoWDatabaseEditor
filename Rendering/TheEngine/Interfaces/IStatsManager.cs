using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Interfaces
{
    public struct RenderStats
    {
        public int ShaderSwitches;
        public int MaterialActivations;
        public int MeshSwitches;
        public int InstancedDraws;
        public int NonInstancedDraws;
        public int InstancedDrawSaved;
        public int TrianglesDrawn;
        public int IndicesDrawn;
    }
    
    public struct PerformanceCounters
    {
        public RollingAverage BoundsCalc;
        public RollingAverage Culling;
        public RollingAverage Drawing;
        public RollingAverage TotalRender;
        public RollingAverage FrameTime;
        public RollingAverage UpdateTime;
        public RollingAverage PresentTime;
        public RollingAverage Sorting;
    }
    
    public interface IStatsManager
    {
        public ref PerformanceCounters Counters { get; }
        public Vector2 PixelSize { get; }
        public ref RenderStats RenderStats { get; }
    }
}