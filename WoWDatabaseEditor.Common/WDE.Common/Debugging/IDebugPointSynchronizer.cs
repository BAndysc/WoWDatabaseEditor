using System.Threading.Tasks;

namespace WDE.Common.Debugging;

public enum SynchronizationResult
{
    // returned if the debug point was successfully synchronized with the server
    Ok,
    // returned if the debug point was not synchronized with the server because the debugged element needs to be saved first
    OutOfSync
}

public interface IDebugPointSynchronizer
{
    Task<SynchronizationResult> Synchronize(DebugPointId id);
    Task Delete(DebugPointId id);
}