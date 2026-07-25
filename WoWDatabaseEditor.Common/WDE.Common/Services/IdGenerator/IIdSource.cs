using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.IdGenerator;

/// <summary>
/// One strategy of allocating ids of a single kind ("personal guid range", "max + 1", ...).
/// Many sources may serve the same id kind; the user picks the active one per kind.
/// Implement <see cref="IdSource{T}"/> rather than this interface directly.
/// </summary>
[NonUniqueProvider]
public interface IIdSource
{
    /// <summary>Display name of the strategy; also the persisted settings key, keep it stable.</summary>
    string Name { get; }

    /// <summary>The concrete <see cref="IIdType"/> this source allocates.</summary>
    Type IdType { get; }

    /// <summary>False when the source needs user setup first (e.g. no range configured).</summary>
    bool IsConfigured { get; }

    /// <summary>Among sources for the same id kind, the configured one with the highest priority
    /// is the default when the user hasn't picked a source yet (unconfigured sources lose).</summary>
    int DefaultPriority { get; }

    /// <summary>Allocates count consecutive ids, returns the first one.</summary>
    Task<long> GetNext(IIdType request, int count, IdGenerationContext context);

    /// <summary>
    /// Optional settings panel embedded in the "Id generation" configuration page, shown when
    /// this source is selected for its id kind. Null when there is nothing to configure.
    /// The view is resolved by the usual view locator convention from the returned view model.
    /// </summary>
    IIdSourceConfiguration? CreateConfiguration();
}

/// <summary>A source's embeddable settings panel view model.</summary>
public interface IIdSourceConfiguration : INotifyPropertyChanged
{
    bool IsModified { get; }
    void Apply();
}

public abstract class IdSource<T> : IIdSource where T : IIdType
{
    public abstract string Name { get; }

    public Type IdType => typeof(T);

    public virtual bool IsConfigured => true;

    public virtual int DefaultPriority => 0;

    public virtual IIdSourceConfiguration? CreateConfiguration() => null;

    Task<long> IIdSource.GetNext(IIdType request, int count, IdGenerationContext context)
        => GetNext((T)request, count, context);

    protected abstract Task<long> GetNext(T request, int count, IdGenerationContext context);
}

/// <summary>Cross-source state the generator service maintains per id kind.</summary>
public readonly struct IdGenerationContext
{
    /// <summary>
    /// Highest id the generator handed out (or was told about via MarkUsed) this session
    /// for this id kind, null when none. Sources deriving ids from the database state
    /// (e.g. "take max") must allocate above it, otherwise two unsaved documents collide.
    /// </summary>
    public long? MaxUsedId { get; init; }
}
