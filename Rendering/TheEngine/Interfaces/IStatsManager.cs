using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Interfaces
{
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
    }
}