using TheEngine;
using WDE.Module.Attributes;

namespace WDE.MapRenderer.Managers;

/// <summary>
/// Arbitrates who owns the in-world pointer across the map modules (the spawn gizmo, the waypoint
/// and formation editors, ...). Two concepts: a multi-frame exclusive <see cref="CaptureLease"/>
/// (gizmo drag, rotation, editor drag) and a one-shot per-frame "pointer used" signal for discrete
/// clicks. Modules that run later in the same frame check these and stand down.
/// </summary>
[UniqueProvider]
public interface IWorldInteractionService
{
    /// <summary>True while a modal interaction owns the world pointer. Passive pickers must stand down.</summary>
    bool IsCaptured { get; }

    /// <summary>Begins an exclusive multi-frame interaction. Returns false (and leaves <paramref name="lease"/>
    /// default) if one is already active. Hold the lease in a field and <see cref="CaptureLease.Release"/>
    /// it when the interaction ends.</summary>
    bool TryCapture(out CaptureLease lease);

    /// <summary>True if the world pointer was already handled this frame (a one-shot pick) or is captured,
    /// so later consumers in the same frame should skip it.</summary>
    bool PointerUsedThisFrame { get; }

    /// <summary>Marks the world pointer as handled for the current frame (one-shot picks/selects).</summary>
    void UsePointerThisFrame();
}

// registered as a singleton in Game.cs, alongside the other per-3D-view services (RaycastSystem,
// ChangesManager, ...), so it resolves in both the app and the standalone rendering testers
public sealed class WorldInteractionService : IWorldInteractionService
{
    private readonly Engine engine;

    private bool captured;
    private int generation; // stale leases don't match on Release()/IsActive

    private long usedFrame = -1;

    public WorldInteractionService(Engine engine)
    {
        this.engine = engine;
    }

    public bool IsCaptured => captured;

    public bool TryCapture(out CaptureLease lease)
    {
        if (captured)
        {
            lease = default;
            return false;
        }

        captured = true;
        generation++;
        lease = new CaptureLease(this, generation);
        return true;
    }

    public bool PointerUsedThisFrame => captured || usedFrame == engine.FrameCount;

    public void UsePointerThisFrame() => usedFrame = engine.FrameCount;

    internal bool IsLeaseActive(int leaseGeneration) => captured && leaseGeneration == generation;

    internal void Release(int leaseGeneration)
    {
        if (captured && leaseGeneration == generation)
        {
            captured = false;
            generation++; // invalidate the just-released lease
        }
    }
}

/// <summary>Alloc-free (service, generation) value-type handle returned by <see cref="IWorldInteractionService.TryCapture"/>. Store in a field; never box.</summary>
public readonly struct CaptureLease
{
    private readonly WorldInteractionService? service;
    private readonly int generation;

    internal CaptureLease(WorldInteractionService service, int generation)
    {
        this.service = service;
        this.generation = generation;
    }

    /// <summary>True while this lease is still the active capture (false once released or stolen).</summary>
    public bool IsActive => service != null && service.IsLeaseActive(generation);

    /// <summary>Ends the capture if this lease still owns it; a no-op otherwise.</summary>
    public void Release() => service?.Release(generation);
}
