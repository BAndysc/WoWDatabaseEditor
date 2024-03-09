
namespace RenderingTester;

public sealed class SingleThreadSynchronizationContext : SynchronizationContext
{
    private const int InitialCapacity = 20;
    private readonly List<WorkRequest> asyncWorkQueue;
    private readonly List<WorkRequest> currentFrameWork = new(InitialCapacity);
    private readonly int mainThreadId;
    private int trackedCount = 0;

    public SingleThreadSynchronizationContext(int mainThreadId)
    {
        asyncWorkQueue = new List<WorkRequest>(InitialCapacity);
        this.mainThreadId = mainThreadId;
    }

    private SingleThreadSynchronizationContext(List<WorkRequest> queue, int mainThreadId)
    {
        asyncWorkQueue = queue;
        this.mainThreadId = mainThreadId;
    }

    // Send will process the call synchronously. If the call is processed on the main thread, we'll invoke it
    // directly here. If the call is processed on another thread it will be queued up like POST to be executed
    // on the main thread and it will wait. Once the main thread processes the work we can continue
    public override void Send(SendOrPostCallback callback, object? state)
    {
        if (mainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            callback(state);
        }
        else
        {
            using var waitHandle = new ManualResetEvent(false);
            lock (asyncWorkQueue)
            {
                asyncWorkQueue.Add(new WorkRequest(callback, state, waitHandle));
            }
            waitHandle.WaitOne();
        }
    }

    public override void OperationStarted() { Interlocked.Increment(ref trackedCount); }
    public override void OperationCompleted() { Interlocked.Decrement(ref trackedCount); }

    // Post will add the call to a task list to be executed later on the main thread then work will continue asynchronously
    public override void Post(SendOrPostCallback callback, object? state)
    {
        lock (asyncWorkQueue)
        {
            asyncWorkQueue.Add(new WorkRequest(callback, state));
        }
    }

    // CreateCopy returns a new UnitySynchronizationContext object, but the queue is still shared with the original
    public override SynchronizationContext CreateCopy()
    {
        lock (asyncWorkQueue)
        {
            return new SingleThreadSynchronizationContext(asyncWorkQueue, mainThreadId);
        }
    }

    // Exec will execute tasks off the task list
    public void ExecuteTasks()
    {
        lock (asyncWorkQueue)
        {
            currentFrameWork.AddRange(asyncWorkQueue);
            asyncWorkQueue.Clear();
        }

        // When you invoke work, remove it from the list to stop it being triggered again
        while (currentFrameWork.Count > 0)
        {
            WorkRequest work = currentFrameWork[0];
            currentFrameWork.RemoveAt(0);
            work.Invoke();
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