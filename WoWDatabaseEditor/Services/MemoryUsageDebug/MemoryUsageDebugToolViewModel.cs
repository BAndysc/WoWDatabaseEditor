using System;
using System.Diagnostics;
using System.Reactive;
using System.Runtime.Versioning;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.MemoryUsageDebug;

[AutoRegister]
[RequiresPreviewFeatures]
public partial class MemoryUsageDebugToolViewModel : ObservableBase, ITool
{
    [Notify] private bool visibility;
    [Notify] private bool isSelected;
    
    public string Title => "Debug memory usage";
    public string UniqueId => "debug_memory_usage";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
    public bool OpenOnStart => false;

    private IDisposable? refreshDataTimer;
    private Process? currentProcess;
    [Notify] private long heapBytes;
    [Notify] private long allocatedBytesPerSecond;
    [Notify] private long totalMemory;
    [Notify] private long totalGCMemory;

    public FloatGraph HeapBytesGraph { get; } = new(100, 100_000_000);
    public FloatGraph AllocatedBytesPerSecondGraph { get; } = new(100, 100_000_000);
    public FloatGraph TotalMemoryGraph { get; } = new(100, 100_000_000);
    public FloatGraph TotalGCMemoryGraph { get; } = new(100, 100_000_000);
    
    public ICommand ForceGC { get; }
    
    private DateTime? lastRefreshTime;
    private long? lastTotalAllocatedMemory;
    
    private RollingAverage updateTimeAverage = new();
    private RollingAverage newAllocatedMemory = new();

    public MemoryUsageDebugToolViewModel(IMainThread mainThread)
    {
        ForceGC = new DelegateCommand(() =>
        {
            for (int i = 0; i < 10; ++i)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }
        });
        
        On(() => Visibility, value =>
        {
            if (value)
            {
                lastTotalAllocatedMemory = GC.GetTotalAllocatedBytes();
                lastRefreshTime = null;
                currentProcess = Process.GetCurrentProcess();
                refreshDataTimer = mainThread.StartTimer(UpdateMemory, TimeSpan.FromMilliseconds(250));
            }
            else
            {
                currentProcess?.Dispose();
                currentProcess = null;
                refreshDataTimer?.Dispose();
                refreshDataTimer = null;
            }
        });
    }

    private bool UpdateMemory()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        
        var now = DateTime.Now;

        var nowTotalAllocatedBytes = GC.GetTotalAllocatedBytes();
        
        HeapBytes = gcInfo.HeapSizeBytes;
        HeapBytesGraph.Add(HeapBytes);
        currentProcess!.Refresh();
        TotalMemory = currentProcess.PrivateMemorySize64;
        TotalMemoryGraph.Add(totalMemory);
        TotalGCMemory = GC.GetTotalMemory(false);
        TotalGCMemoryGraph.Add(totalGCMemory);

        if (lastTotalAllocatedMemory.HasValue && lastRefreshTime.HasValue)
        {
            newAllocatedMemory.Add(nowTotalAllocatedBytes - lastTotalAllocatedMemory.Value);
            updateTimeAverage.Add((long)(now - lastRefreshTime.Value).TotalMilliseconds);
            AllocatedBytesPerSecond = (long)(newAllocatedMemory.Sum * 1000 / updateTimeAverage.Sum);
            AllocatedBytesPerSecondGraph.Add(AllocatedBytesPerSecond);
        }

        lastRefreshTime = now;
        lastTotalAllocatedMemory = nowTotalAllocatedBytes;
        
        return true;
    }

    public partial class FloatGraph
    {
        private readonly float minMinus;
        private float[] points;
        private int i;
        [Notify] private int offsetStart;
        [Notify] private float min = float.MaxValue;
        [Notify] private float max = float.MinValue;
        [Notify] private int elements = 0;

        public float[] Memory => points;
        
        public FloatGraph(int pointsCount, float minMinus)
        {
            this.minMinus = minMinus;
            points = new float[pointsCount];
        }

        public void Add(float t)
        {
            points[i] = t;
            if (elements < points.Length)
                Elements++;
            else
            {
                OffsetStart++;
                if (OffsetStart == Elements)
                    OffsetStart = 0;
            }
            i += 1;
            i %= points.Length;
            Min = float.MaxValue;
            Max = float.MinValue;
            foreach (var v in points.AsSpan(0, elements))
            {
                Min = Math.Max(0, Math.Min(min, v- minMinus));
                Max = Math.Max(max, v);
            }
        }
    }
    
    public struct RollingAverage
    {
        private int i = 0;
        private int filledValues = 0;
        private double[] buffer = new double[32];
        private double sum = 0;
        private double avg = 0;

        public RollingAverage()
        {
            
        }
        
        public double Average => avg;

        public double Sum => sum;

        public void Add(double t)
        {
            sum += t - buffer[i];
            if (filledValues == 0)
                avg = t;
            else
                avg += (t - buffer[i]) / filledValues;
            buffer[i] = t;
            i += 1;
            i %= 32;
            if (filledValues < 32)
                filledValues++;
        }
    }
}