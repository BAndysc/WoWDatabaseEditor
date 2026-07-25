using TheEngine.Utils;
using WDE.MapRenderer;

namespace RenderingTester;

public class CustomSynchronizationContext : SynchronizationContext, IGameModule
{
    private DoubleBufferedList<(SendOrPostCallback, object?)> work = new();

    public override void Post(SendOrPostCallback d, object? state)
    {
        work.Add((d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        d(state);
    }

    public void Dispose()
    {
    }

    public object? ViewModel { get; set; }
    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        var tasks = work.Collect();
        try
        {
            foreach (var t in tasks)
            {
                t.Item1(t.Item2);
            }
        }
        finally
        {
            tasks.Clear();
        }
    }
}