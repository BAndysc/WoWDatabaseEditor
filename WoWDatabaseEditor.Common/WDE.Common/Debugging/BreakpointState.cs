namespace WDE.Common.Debugging;

public enum BreakpointState
{
    /// <summary>
    /// Waiting for the sync to the server.
    /// It will be synced upon next Synchronize call.
    /// </summary>
    Pending,
    /// <summary>
    /// Waiting for being removed from the server.
    /// It will be synced upon next Synchronize call.
    /// </summary>
    PendingRemoval,
    /// <summary>
    /// The state is synced with the server
    /// </summary>
    Synced,
    /// <summary>
    /// Due to source mismatch, the breakpoint is in a stale state
    /// It won't be synced until its state is changed to Pending
    /// </summary>
    WaitingForSync,
    /// <summary>
    /// There was an error during synchronization
    /// </summary>
    SynchronizationError
}