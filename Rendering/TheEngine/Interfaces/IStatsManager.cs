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
        public int Set1CacheHits;
        public int Set1CacheMisses;
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
        // CPU time spent blocked at the start of the frame: waiting on the previous frame's fence
        // and acquiring the next swapchain image. High here (with low GPU time) means the frame is
        // idle-waiting (vsync/present-bound or back-pressured), not doing real render work.
        public RollingAverage GpuFenceWait;
        public RollingAverage GpuAcquire;
        // CPU time blocked in the low-latency vsync throttle (vkWaitForPresentKHR): waiting for the
        // previous present to reach the display before sampling this frame's input. High here with
        // vsync on is expected and healthy - it's latency moved out of the present queue.
        public RollingAverage GpuPresentWait;
        // Per-phase CPU time of the render section (the same buckets THEENGINE_PROFILE prints). The
        // bounds/culling/sorting/drawing counters above are only sub-slices of the object stage; these
        // cover the whole frame so unaccounted render time is attributable to a concrete phase.
        public RollingAverage RenderBegin;
        public RollingAverage RenderPrepare;
        public RollingAverage RenderOpaquePhase;
        public RollingAverage RenderTransparentPhase;
        // the game's own opaque+transparent draw callbacks (untimed by the phases above)
        public RollingAverage RenderGameCustom;
        public RollingAverage RenderPostProcessPhase;
        public RollingAverage RenderGui;
        public RollingAverage RenderFinalize;
        public RollingAverage RenderEnd;
    }
    
    public interface IStatsManager
    {
        public ref PerformanceCounters Counters { get; }
        public Vector2 PixelSize { get; }
        public ref RenderStats RenderStats { get; }
        public ulong TextureBytes { get; }
        public ulong EntitiesUnmanagedBytes { get; }
        public long BufferBytes { get; }
        // GPU memory churn for the current frame (resource-level create/destroy, and the rarer
        // device-level block vkAllocateMemory/vkFreeMemory which should stay at 0 in steady state).
        public int GpuAllocationsPerFrame { get; }
        public int GpuFreesPerFrame { get; }
        public int GpuDeviceAllocationsPerFrame { get; }
        public int GpuDeviceFreesPerFrame { get; }
    }
}