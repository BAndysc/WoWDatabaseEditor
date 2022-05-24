using TheEngine.Utils;

namespace TheEngine;

public static class TheEngine
{
    public static void Deinit()
    {
        NaiveThreadPool.Pool.Dispose();
    }
}