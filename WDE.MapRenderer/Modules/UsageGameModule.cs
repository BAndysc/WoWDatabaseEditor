using TheEngine;
using TheEngine.Interfaces;
using WDE.Common;

namespace WDE.MapRenderer.Modules;

/// <summary>
/// Reports how the 3D view performs on the user's machine: a "3d_perf" event
/// with the median FPS bucket and the render backend/GPU name, sent every
/// 10 minutes of the 3D view being open and when it closes.
/// USAGE is thread-safe by design, so calling it from the game thread is fine.
/// </summary>
public class UsageGameModule : IGameModule
{
    private const float SampleIntervalSeconds = 2;
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMinutes(10);
    private const int MaxSamples = 10000;

    private readonly IStatsManager statsManager;
    private readonly Engine engine;

    private readonly List<float> fpsSamples = new();
    private float timeUntilNextSample = SampleIntervalSeconds;
    private DateTime lastReportTime = DateTime.UtcNow;

    public object? ViewModel => null;

    public UsageGameModule(IStatsManager statsManager, Engine engine)
    {
        this.statsManager = statsManager;
        this.engine = engine;
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        timeUntilNextSample -= delta;
        if (timeUntilNextSample > 0)
            return;
        timeUntilNextSample = SampleIntervalSeconds;

        var frameTimeMs = statsManager.Counters.FrameTime.Average;
        if (frameTimeMs > 0 && fpsSamples.Count < MaxSamples)
            fpsSamples.Add(1000.0f / (float)frameTimeMs);

        if (DateTime.UtcNow - lastReportTime >= ReportInterval)
            Report();
    }

    public void Dispose()
    {
        Report();
    }

    private void Report()
    {
        lastReportTime = DateTime.UtcNow;
        if (fpsSamples.Count < 5)
            return;

        fpsSamples.Sort();
        var median = fpsSamples[fpsSamples.Count / 2];
        fpsSamples.Clear();

        USAGE.Event("3d_perf",
            ("fps_median", FpsBucket(median)),
            ("gpu", engine.BackendName));
    }

    private static string FpsBucket(float fps)
    {
        if (fps < 15) return "<15";
        if (fps < 30) return "15-30";
        if (fps < 60) return "30-60";
        if (fps < 120) return "60-120";
        return "120+";
    }
}
