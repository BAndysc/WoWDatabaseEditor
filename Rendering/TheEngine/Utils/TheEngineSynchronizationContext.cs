namespace TheEngine.Utils;

public sealed class TheEngineSynchronizationContext : SynchronizationContext
{
    private readonly Engine engine;
    private readonly int mainThreadId;
    private int trackedCount = 0;
    private DoubleBufferedList<WorkRequest> work;

    public TheEngineSynchronizationContext(Engine engine, int mainThreadId)
    {
        this.engine = engine;
        this.mainThreadId = mainThreadId;
        work = new();
    }

    private TheEngineSynchronizationContext(TheEngineSynchronizationContext parent)
    {
        this.engine = parent.engine;
        this.mainThreadId = parent.mainThreadId;
        this.work = parent.work;
    }

    // Send will process the call synchronously. If the call is processed on the main thread, we'll invoke it
    // directly here. If the call is processed on another thread it will be queued up like POST to be executed
    // on the main thread and it will wait. Once the main thread processes the work we can continue
    public override void Send(SendOrPostCallback callback, object? state)
    {
        if (engine.InFrame && mainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            callback(state);
        }
        else
        {
            using var waitHandle = new ManualResetEvent(false);
            work.Add(new WorkRequest(callback, state, waitHandle));
            waitHandle.WaitOne();
        }
    }

    public override void OperationStarted() { Interlocked.Increment(ref trackedCount); }
    public override void OperationCompleted() { Interlocked.Decrement(ref trackedCount); }

    // Post will add the call to a task list to be executed later on the main thread then work will continue asynchronously
    public override void Post(SendOrPostCallback callback, object state)
    {
        work.Add(new WorkRequest(callback, state));
    }

    // CreateCopy returns a new UnitySynchronizationContext object, but the queue is still shared with the original
    public override SynchronizationContext CreateCopy()
    {
        return new TheEngineSynchronizationContext(this);
    }

    // Exec will execute tasks off the task list
    public void ExecuteTasks()
    {
        var workToDo = work.Collect();
        try
        {
            foreach (var todo in workToDo)
            {
                todo.Invoke();
            }
        }
        finally
        {
            workToDo.Clear();
        }
    }

    private readonly struct WorkRequest
    {
        private readonly SendOrPostCallback delegateCallback;
        private readonly object? delegateState;
        private readonly ManualResetEvent? waitHandle;

        public WorkRequest(SendOrPostCallback callback, object? state, ManualResetEvent? waitHandle = null)
        {
            delegateCallback = callback;
            delegateState = state;
            this.waitHandle = waitHandle;
        }

        public void Invoke()
        {
            try
            {
                delegateCallback(delegateState);
            }
            finally
            {
                waitHandle?.Set();
            }
        }
    }
}