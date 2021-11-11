using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    public class StatsManager : IStatsManager
    {
        private PerformanceCounters counters;
        private RenderStats renderStats;
        public ref PerformanceCounters Counters => ref counters;
        
        public Vector2 PixelSize { get; internal set; }

        public ref RenderStats RenderStats => ref renderStats;
    }
}