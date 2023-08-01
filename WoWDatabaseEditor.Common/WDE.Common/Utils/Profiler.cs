using System;
using JetBrains.Profiler.Api;
using WDE.Common.Tasks;

namespace WDE.Common.Utils;

public static class Profiler
{
    // a dirty workaround, but this is only used for debuggin
    private static IMainThread? mainThread;

    public static void SetupMainThread(IMainThread _mainThread)
    {
        mainThread = _mainThread;
    }

    public static void Start()
    {
        MeasureProfiler.StartCollectingData();
    }
    
    public static void Stop()
    {
        MeasureProfiler.StopCollectingData();
        MeasureProfiler.SaveData();
    }
    
    public static void ProfileOneFrame()
    {
        if (mainThread == null)
            throw new Exception("Set MainThread before using."); 
        
        MeasureProfiler.StartCollectingData();
        mainThread.Delay(() =>
        {
            MeasureProfiler.StopCollectingData();
            MeasureProfiler.SaveData();
        }, TimeSpan.FromMilliseconds(16));
    }
}