namespace WDE.SqlWorkbench.Test.IntegrationTests;

public class ManualSynchronizationContext : SynchronizationContext
{
    private List<(SendOrPostCallback d, object? state)> queue = new();
    
    public override void Post(SendOrPostCallback d, object? state)
    {
        queue.Add((d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        base.Send(d, state);
    }
    
    public void ExecuteAll()
    {
        var copy = queue.ToArray();
        queue.Clear();
        foreach (var (d, state) in copy)
            d(state);
    }
}