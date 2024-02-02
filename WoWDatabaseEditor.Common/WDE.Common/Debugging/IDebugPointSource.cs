using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WDE.Module.Attributes;

namespace WDE.Common.Debugging;

[NonUniqueProvider]
public interface IDebugPointSource
{
    /// <summary>
    /// Human friendly name of the source of the breakpoints
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Unique key for serialization
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Features of this breakpoints provider
    /// </summary>
    DebugSourceFeatures Features { get; }
    Task CreateDebugPoint();
    IDebugPointSynchronizer Synchronizer { get; }
    string GenerateName(DebugPointId id);
    string? GetAllowedCodeCompletionsRoot(DebugPointId id);
    Task FetchFromServerAsync(CancellationToken cancellationToken);
    Task ForceClearAllAsync();
    object? GeneratePayloadViewModel(DebugPointId id);
    JObject? SerializePayload(DebugPointId id);
    IDebugPointPayload DeserializePayload(JObject payload);
    bool IsBreakpointHit(IdeBreakpointHitEventArgs hitArgs, DebugPointId id, IDebugPointPayload payload);
}

[Flags]
public enum DebugSourceFeatures
{
    CanCreateDebugPoint = 1,
    CanChangeSuspendExecution = 2,
    CanChangeGenerateStacktrace = 4,
}